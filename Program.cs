using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace who_admin
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        TextBox txtComputers;
        Button btnLoadFromAD, btnScan, btnExport, btnStop;
        CheckBox chkExpandDomainGroups;
        NumericUpDown nudParallel;
        DataGridView grid;
        ProgressBar progress;
        Label lblStatus;

        CancellationTokenSource? cts;

        public MainForm()
        {
            Text = "Сканер: члены локальной группы Администраторы";
            Width = 1100;
            Height = 700;

            txtComputers = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Width = 350, Height = 500, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom };
            btnLoadFromAD = new Button { Text = "Загрузить ПК из AD", Width = 160, Top = 510 };
            btnScan = new Button { Text = "Сканировать", Left = 170, Top = 510, Width = 120 };
            btnStop = new Button { Text = "Стоп", Left = 295, Top = 510, Width = 55, Enabled = false };
            chkExpandDomainGroups = new CheckBox { Text = "Разворачивать доменные группы", Top = 545, Width = 300 };
            nudParallel = new NumericUpDown { Minimum = 1, Maximum = 128, Value = Math.Max(1, Environment.ProcessorCount - 0), Left = 300, Top = 545, Width = 60 };
            btnExport = new Button { Text = "Экспорт CSV", Left = 370, Top = 545, Width = 120 };

            grid = new DataGridView
            {
                Left = 360,
                Width = 710,
                Height = 500,
                Top = 0,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            grid.Columns.Add("Computer", "Компьютер");
            grid.Columns.Add("Status", "Статус");
            grid.Columns.Add("MemberType", "Тип члена");
            grid.Columns.Add("Account", "Учетная запись");
            grid.Columns.Add("Source", "Источник");
            grid.Columns.Add("ExpandedFrom", "Развёрнуто из");

            progress = new ProgressBar { Left = 0, Top = 580, Width = 1070, Height = 20, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            lblStatus = new Label { Left = 0, Top = 605, Width = 1070, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };

            Controls.AddRange(new Control[] {
                txtComputers, btnLoadFromAD, btnScan, btnStop, chkExpandDomainGroups, nudParallel, btnExport,
                grid, progress, lblStatus
            });

            btnLoadFromAD.Click += async (s, e) => await LoadComputersFromAD();
            btnScan.Click += async (s, e) => await ScanAsync();
            btnStop.Click += (s, e) => cts?.Cancel();
            btnExport.Click += (s, e) => ExportCsv();
        }

        async Task LoadComputersFromAD()
        {
            try
            {
                UseWaitCursor = true;
                btnLoadFromAD.Enabled = false;

                var list = await Task.Run(() => QueryComputersFromAD());
                if (list.Count == 0)
                {
                    MessageBox.Show("В AD не найдено подходящих рабочих станций (или нет доступа).", "AD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                txtComputers.Text = string.Join(Environment.NewLine, list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка AD: " + ex.Message, "AD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLoadFromAD.Enabled = true;
                UseWaitCursor = false;
            }
        }

        List<string> QueryComputersFromAD()
        {
            // Берём корень домена по RootDSE
            using (var rootDse = new DirectoryEntry("LDAP://RootDSE"))
            {
                string defaultNc = rootDse.Properties["defaultNamingContext"].Value?.ToString() ?? "";
                using (var searchRoot = new DirectoryEntry("LDAP://" + defaultNc))
                using (var ds = new DirectorySearcher(searchRoot))
                {
                    // Только включённые рабочие станции (без Server)
                    ds.PageSize = 1000;
                    ds.Filter = "(&(objectCategory=computer)(!(userAccountControl:1.2.840.113556.1.4.803:=2))(!(operatingSystem=*Server*)))";
                    ds.PropertiesToLoad.Add("name");
                    var results = ds.FindAll();
                    var list = new List<string>();
                    foreach (SearchResult r in results)
                    {
                        if (r.Properties.Contains("name"))
                            list.Add(r.Properties["name"][0]?.ToString() ?? "");
                    }
                    list.Sort(StringComparer.OrdinalIgnoreCase);
                    return list;
                }
            }
        }

        async Task ScanAsync()
        {
            var computers = txtComputers.Text
                .Split(new[] { '\r', '\n', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (computers.Count == 0)
            {
                MessageBox.Show("Добавь имена компьютеров (по одному в строке) или загрузи из AD.", "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            grid.Rows.Clear();
            cts = new CancellationTokenSource();
            btnScan.Enabled = false;
            btnStop.Enabled = true;
            btnLoadFromAD.Enabled = false;
            UseWaitCursor = true;

            progress.Minimum = 0;
            progress.Maximum = computers.Count;
            progress.Value = 0;
            lblStatus.Text = $"Начинаю сканирование {computers.Count} ПК…";

            var bag = new ConcurrentBag<ResultRow>();
            var errors = new ConcurrentBag<ResultRow>();
            int done = 0;
            int dop = (int)nudParallel.Value;

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(
                        computers,
                        new ParallelOptions { CancellationToken = cts.Token, MaxDegreeOfParallelism = dop },
                        computer =>
                        {
                            try
                            {
                                var rows = ScanComputer(computer, chkExpandDomainGroups.Checked);
                                foreach (var r in rows) bag.Add(r);
                                if (!rows.Any())
                                {
                                    bag.Add(new ResultRow
                                    {
                                        Computer = computer,
                                        Status = "OK (нет прямых членов)",
                                        MemberType = "",
                                        Account = "",
                                        Source = "Win32_GroupUser",
                                        ExpandedFrom = ""
                                    });
                                }
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                errors.Add(new ResultRow
                                {
                                    Computer = computer,
                                    Status = "Ошибка: " + ex.Message,
                                    MemberType = "",
                                    Account = "",
                                    Source = "WMI",
                                    ExpandedFrom = ""
                                });
                            }
                            finally
                            {
                                Interlocked.Increment(ref done);
                                Invoke(new Action(() =>
                                {
                                    progress.Value = Math.Min(progress.Maximum, done);
                                    lblStatus.Text = $"Готово {done}/{computers.Count}";
                                }));
                            }
                        });
                }, cts.Token);

                // апдейт таблицы
                var all = bag.Concat(errors)
                             .OrderBy(r => r.Computer, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(r => r.Account, StringComparer.OrdinalIgnoreCase)
                             .ToList();

                foreach (var r in all)
                {
                    grid.Rows.Add(r.Computer, r.Status, r.MemberType, r.Account, r.Source, r.ExpandedFrom);
                }

                lblStatus.Text = $"Готово: {computers.Count} ПК. Ошибок: {errors.Count}.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Остановлено пользователем.";
            }
            finally
            {
                btnScan.Enabled = true;
                btnStop.Enabled = false;
                btnLoadFromAD.Enabled = true;
                UseWaitCursor = false;
                cts?.Dispose();
                cts = null;
            }
        }

        static List<ResultRow> ScanComputer(string computer, bool expandDomainGroups)
        {
            var results = new List<ResultRow>();

            var scope = new ManagementScope($@"\\{computer}\root\cimv2");
            scope.Connect(); // бросит исключение при недоступности

            // Находим локальную группу Администраторы по её универсальному SID
            var grpSearcher = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM Win32_Group WHERE LocalAccount=True AND SID='S-1-5-32-544'"));

            var groups = grpSearcher.Get().Cast<ManagementObject>().ToList();
            if (groups.Count == 0)
                throw new Exception("Не найдена локальная группа S-1-5-32-544 (Администраторы)");

            var group = groups[0];
            string groupName = (string?)group["Name"] ?? "";
            string groupDomain = (string?)group["Domain"] ?? "";

            // Получаем членов через ассоциацию
            string assocQuery =
                $"ASSOCIATORS OF {{Win32_Group.Domain='{EscapeWmi(groupDomain)}',Name='{EscapeWmi(groupName)}'}} " +
                "WHERE AssocClass=Win32_GroupUser Role=GroupComponent";

            var assoc = new ManagementObjectSearcher(scope, new ObjectQuery(assocQuery)).Get();

            var directMembers = new List<(string Class, string Domain, string Name)>();
            foreach (ManagementObject mo in assoc)
            {
                string cls = mo.ClassPath.ClassName; // Win32_UserAccount или Win32_Group
                string mDomain = (string?)mo["Domain"] ?? "";
                string mName = (string?)mo["Name"] ?? "";
                directMembers.Add((cls, mDomain, mName));

                // Определяем тип более детально
                string memberType;
                string accountInfo = $"{mDomain}\\{mName}";
                
                if (cls == "Win32_Group")
                {
                    memberType = mDomain.Equals(groupDomain, StringComparison.OrdinalIgnoreCase) 
                        ? "Локальная группа" 
                        : "Доменная группа";
                }
                else
                {
                    memberType = mDomain.Equals(groupDomain, StringComparison.OrdinalIgnoreCase) 
                        ? "Локальный пользователь" 
                        : "Доменный пользователь";
                        
                    // Для пользователей добавляем информацию о статусе
                    try
                    {
                        bool? disabled = (bool?)mo["Disabled"];
                        bool? lockout = (bool?)mo["Lockout"];
                        
                        if (disabled == true)
                            accountInfo += " [ОТКЛЮЧЁН]";
                        else if (lockout == true)
                            accountInfo += " [ЗАБЛОКИРОВАН]";
                    }
                    catch { /* игнорируем ошибки получения статуса */ }
                }

                results.Add(new ResultRow
                {
                    Computer = computer,
                    Status = "OK",
                    MemberType = memberType,
                    Account = accountInfo,
                    Source = "Win32_GroupUser",
                    ExpandedFrom = ""
                });
            }

            if (expandDomainGroups)
            {
                // Разворачиваем только доменные группы (Domain != имя компьютера)
                foreach (var m in directMembers.Where(x => x.Class == "Win32_Group" && !x.Domain.Equals(groupDomain, StringComparison.OrdinalIgnoreCase)))
                {
                    var expandedMembers = ExpandDomainGroupMembersSafe(m.Domain, m.Name);
                    foreach (var memberInfo in expandedMembers)
                    {
                        results.Add(new ResultRow
                        {
                            Computer = computer,
                            Status = "OK",
                            MemberType = memberInfo.Type,
                            Account = memberInfo.Account,
                            Source = "AD (recursive)",
                            ExpandedFrom = $"{m.Domain}\\{m.Name}"
                        });
                    }
                }
            }

            return results;
        }

        static List<MemberInfo> ExpandDomainGroupMembersSafe(string domainNetbios, string groupSam)
        {
            var results = new List<MemberInfo>();
            // Пытаемся найти через контекст домена. Часто достаточно NetBIOS имени,
            // но если у тебя домен в формате DNS, можно подменить здесь на FQDN.
            try
            {
                using (var ctx = new PrincipalContext(ContextType.Domain, domainNetbios))
                using (var gp = GroupPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, groupSam))
                {
                    if (gp == null) return results;
                    foreach (var p in gp.GetMembers(true))
                    {
                        try
                        {
                            string memberType = "Неизвестный тип";
                            string account = "";
                            
                            if (p is UserPrincipal up)
                            {
                                memberType = "Доменный пользователь (из группы)";
                                string dom = up.Context?.Name ?? domainNetbios;
                                string sam = up.SamAccountName ?? up.Name ?? "";
                                account = $"{dom}\\{sam}";
                            }
                            else if (p is GroupPrincipal grp)
                            {
                                memberType = "Доменная группа (вложенная)";
                                string dom = grp.Context?.Name ?? domainNetbios;
                                string sam = grp.SamAccountName ?? grp.Name ?? "";
                                account = $"{dom}\\{sam}";
                            }
                            else if (p is ComputerPrincipal comp)
                            {
                                memberType = "Компьютер домена";
                                string dom = comp.Context?.Name ?? domainNetbios;
                                string sam = comp.SamAccountName ?? comp.Name ?? "";
                                account = $"{dom}\\{sam}";
                            }

                            if (!string.IsNullOrEmpty(account))
                            {
                                results.Add(new MemberInfo { Type = memberType, Account = account });
                            }
                        }
                        catch { /* пропускаем сбойные объекты */ }
                        finally { p.Dispose(); }
                    }
                }
            }
            catch
            {
                // нет доступа к AD или группа неразрешима — молча пропускаем
            }
            return results;
        }

        static string EscapeWmi(string s) => s.Replace(@"\", @"\\").Replace("'", "''");

        void ExportCsv()
        {
            if (grid.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new StringBuilder();
            // Заголовок
            sb.AppendLine("Computer;Status;MemberType;Account;Source;ExpandedFrom");

            foreach (DataGridViewRow row in grid.Rows)
            {
                string[] cells = new string[grid.Columns.Count];
                for (int i = 0; i < grid.Columns.Count; i++)
                {
                    var val = row.Cells[i].Value?.ToString() ?? "";
                    // Экраним ; и "
                    if (val.Contains(";") || val.Contains("\""))
                        val = "\"" + val.Replace("\"", "\"\"") + "\"";
                    cells[i] = val;
                }
                sb.AppendLine(string.Join(";", cells));
            }

            using (var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "LocalAdmins.csv" })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Экспортировано: " + sfd.FileName, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        class ResultRow
        {
            public string Computer { get; set; } = "";
            public string Status { get; set; } = "";
            public string MemberType { get; set; } = "";
            public string Account { get; set; } = "";
            public string Source { get; set; } = "";
            public string ExpandedFrom { get; set; } = "";
        }

        class MemberInfo
        {
            public string Type { get; set; } = "";
            public string Account { get; set; } = "";
        }
    }
}