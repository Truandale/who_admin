using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
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
                                    Source = "NetAPI32",
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

            // Используем NetAPI32 вместо WMI для более надёжного получения членов группы
            var directMembers = new List<(string Account, string Type)>();
            
            try
            {
                var members = LocalAdminsReader.GetMembers(computer);
                foreach (var member in members)
                {
                    directMembers.Add((member.Account, member.Type));
                    
                    results.Add(new ResultRow
                    {
                        Computer = computer,
                        Status = "OK",
                        MemberType = member.Type,
                        Account = member.Account,
                        Source = "NetAPI32",
                        ExpandedFrom = ""
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка NetAPI32: {ex.Message}", ex);
            }

            if (expandDomainGroups)
            {
                // Разворачиваем только доменные группы
                foreach (var member in directMembers.Where(x => x.Type.Contains("Доменная группа")))
                {
                    try
                    {
                        // Извлекаем домен и имя группы из "DOMAIN\GroupName"
                        var parts = member.Account.Split('\\');
                        if (parts.Length == 2)
                        {
                            string domain = parts[0];
                            string groupName = parts[1];
                            
                            var expandedMembers = ExpandDomainGroupMembersSafe(domain, groupName);
                            foreach (var memberInfo in expandedMembers)
                            {
                                results.Add(new ResultRow
                                {
                                    Computer = computer,
                                    Status = "OK",
                                    MemberType = memberInfo.Type,
                                    Account = memberInfo.Account,
                                    Source = "AD (recursive)",
                                    ExpandedFrom = member.Account
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки разворачивания отдельных групп
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

    static class LocalAdminsReader
    {
        // --- WinAPI enums/structs ---
        enum SID_NAME_USE : int
        {
            SidTypeUser = 1, SidTypeGroup, SidTypeDomain, SidTypeAlias, SidTypeWellKnownGroup,
            SidTypeDeletedAccount, SidTypeInvalid, SidTypeUnknown, SidTypeComputer, SidTypeLabel
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct LOCALGROUP_MEMBERS_INFO_2
        {
            public IntPtr lgrmi2_sid;
            public SID_NAME_USE lgrmi2_sidusage;
            [MarshalAs(UnmanagedType.LPWStr)] public string lgrmi2_domainandname; // "DOMAIN\\Name"
        }

        // --- P/Invoke ---
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetLocalGroupGetMembers(
            string servername, string localgroupname, int level,
            out IntPtr bufptr, int prefmaxlen,
            out int entriesread, out int totalentries, IntPtr resume_handle);

        [DllImport("Netapi32.dll")] static extern int NetApiBufferFree(IntPtr Buffer);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool LookupAccountSid(
            string lpSystemName, IntPtr Sid,
            StringBuilder Name, ref uint cchName,
            StringBuilder ReferencedDomainName, ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        // --- Public API ---
        public static IEnumerable<(string Account, string Type, string ExpandedFrom)> GetMembers(string computer)
        {
            // 1) Получаем локализованное имя группы "Администраторы" по её SID
            string adminsLocalName = GetLocalGroupNameBySid(computer, "S-1-5-32-544");

            // 2) Читаем всех членов через NetLocalGroupGetMembers Level=2
            IntPtr buf = IntPtr.Zero;
            int entries, total;
            int status = NetLocalGroupGetMembers(@"\\" + computer, adminsLocalName, 2, out buf, -1, out entries, out total, IntPtr.Zero);
            if (status != 0) throw new Win32Exception(status);

            try
            {
                int sz = Marshal.SizeOf<LOCALGROUP_MEMBERS_INFO_2>();
                for (int i = 0; i < entries; i++)
                {
                    var item = Marshal.PtrToStructure<LOCALGROUP_MEMBERS_INFO_2>(IntPtr.Add(buf, i * sz));
                    string acc = item.lgrmi2_domainandname ?? SidToString(item.lgrmi2_sid);
                    string type = GetMemberTypeDescription(item.lgrmi2_sidusage, acc, computer);
                    yield return (acc, type, "");
                }
            }
            finally { NetApiBufferFree(buf); }
        }

        // --- Helpers ---
        static string GetLocalGroupNameBySid(string computer, string sidStr)
        {
            // Используем .NET SecurityIdentifier вместо ConvertStringSidToSid для избежания LocalFree
            var sid = new System.Security.Principal.SecurityIdentifier(sidStr);
            byte[] buf = new byte[sid.BinaryLength];
            sid.GetBinaryForm(buf, 0);
            IntPtr pSid = Marshal.AllocHGlobal(buf.Length);
            try
            {
                Marshal.Copy(buf, 0, pSid, buf.Length);
                uint cchName = 0, cchDom = 0;
                LookupAccountSid(computer, pSid, null!, ref cchName, null!, ref cchDom, out _); // размер буферов
                var name = new StringBuilder((int)cchName);
                var dom = new StringBuilder((int)cchDom);
                if (!LookupAccountSid(computer, pSid, name, ref cchName, dom, ref cchDom, out _))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return name.ToString(); // локализованное имя локальной группы на целевом ПК
            }
            finally { Marshal.FreeHGlobal(pSid); }
        }

        static string SidToString(IntPtr sid)
        {
            // На случай "битых" записей без домена\имени
            var si = new System.Security.Principal.SecurityIdentifier(sid);
            return si.Value;
        }

        static string GetMemberTypeDescription(SID_NAME_USE sidUsage, string account, string computer)
        {
            // Определяем, локальная это учётка или доменная
            bool isLocal = account.StartsWith(computer + "\\", StringComparison.OrdinalIgnoreCase);

            string baseType = sidUsage switch
            {
                SID_NAME_USE.SidTypeUser => isLocal ? "Локальный пользователь" : "Доменный пользователь",
                SID_NAME_USE.SidTypeGroup => isLocal ? "Локальная группа" : "Доменная группа",
                SID_NAME_USE.SidTypeAlias => isLocal ? "Локальный алиас" : "Доменный алиас",
                SID_NAME_USE.SidTypeWellKnownGroup => "Встроенная группа",
                SID_NAME_USE.SidTypeComputer => "Компьютер домена",
                _ => "Неизвестный тип"
            };

            // Для доменных пользователей пытаемся получить статус через AD
            if (sidUsage == SID_NAME_USE.SidTypeUser && !isLocal)
            {
                try
                {
                    string statusInfo = GetUserAccountStatus(account);
                    if (!string.IsNullOrEmpty(statusInfo))
                        return $"{baseType} {statusInfo}";
                }
                catch { /* игнорируем ошибки получения статуса */ }
            }

            return baseType;
        }

        static string GetUserAccountStatus(string domainUser)
        {
            try
            {
                var parts = domainUser.Split('\\');
                if (parts.Length != 2) return "";

                string domain = parts[0];
                string username = parts[1];

                using (var ctx = new PrincipalContext(ContextType.Domain, domain))
                using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, username))
                {
                    if (user == null) return "";

                    var flags = new List<string>();
                    if (user.Enabled == false) flags.Add("ОТКЛЮЧЁН");
                    if (user.IsAccountLockedOut()) flags.Add("ЗАБЛОКИРОВАН");
                    if (user.PasswordNeverExpires == true) flags.Add("ПАРОЛЬ НЕ ИСТЕКАЕТ");

                    return flags.Count > 0 ? $"[{string.Join(", ", flags)}]" : "";
                }
            }
            catch
            {
                return "";
            }
        }
    }
}