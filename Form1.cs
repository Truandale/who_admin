using System.Collections.Concurrent;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Text;

namespace who_admin
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource? cts;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Настройка формы
            this.Text = "Сканер: члены локальной группы Администраторы";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Настройка DataGridView
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Добавление колонок
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Computer", "Компьютер");
            dataGridView1.Columns.Add("Status", "Статус");
            dataGridView1.Columns.Add("MemberType", "Тип члена");
            dataGridView1.Columns.Add("Account", "Учетная запись");
            dataGridView1.Columns.Add("Source", "Источник");
            dataGridView1.Columns.Add("ExpandedFrom", "Развёрнуто из");

            // Настройка NumericUpDown
            numericUpDownThreads.Minimum = 1;
            numericUpDownThreads.Maximum = 128;
            numericUpDownThreads.Value = Math.Max(1, Environment.ProcessorCount);

            // Настройка кнопок
            buttonStop.Enabled = false;

            // Привязка событий
            buttonLoadFromAD.Click += async (s, e) => await LoadComputersFromAD();
            buttonScan.Click += async (s, e) => await ScanAsync();
            buttonStop.Click += (s, e) => cts?.Cancel();
            buttonExport.Click += (s, e) => ExportCsv();

            // Настройка статус-бара
            labelStatus.Text = "Готов к работе";
        }

        async Task LoadComputersFromAD()
        {
            try
            {
                UseWaitCursor = true;
                buttonLoadFromAD.Enabled = false;
                labelStatus.Text = "Загрузка компьютеров из AD...";

                var list = await Task.Run(() => QueryComputersFromAD());
                if (list.Count == 0)
                {
                    MessageBox.Show("В AD не найдено подходящих рабочих станций (или нет доступа).", "AD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                textBoxComputers.Text = string.Join(Environment.NewLine, list);
                labelStatus.Text = $"Загружено {list.Count} компьютеров из AD";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка AD: " + ex.Message, "AD", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelStatus.Text = "Ошибка загрузки из AD";
            }
            finally
            {
                buttonLoadFromAD.Enabled = true;
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
            var computers = textBoxComputers.Text
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

            dataGridView1.Rows.Clear();
            cts = new CancellationTokenSource();
            buttonScan.Enabled = false;
            buttonStop.Enabled = true;
            buttonLoadFromAD.Enabled = false;
            UseWaitCursor = true;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = computers.Count;
            progressBar1.Value = 0;
            labelStatus.Text = $"Начинаю сканирование {computers.Count} ПК…";

            var bag = new ConcurrentBag<ResultRow>();
            var errors = new ConcurrentBag<ResultRow>();
            int done = 0;
            int dop = (int)numericUpDownThreads.Value;

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
                                var rows = ScanLogic.ScanComputer(computer, checkBoxExpandGroups.Checked);
                                foreach (var r in rows) bag.Add(r);
                                if (!rows.Any())
                                {
                                    bag.Add(new ResultRow
                                    {
                                        Computer = computer,
                                        Status = "OK (нет прямых членов)",
                                        MemberType = "",
                                        Account = "",
                                        Source = "NetAPI32",
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
                                    progressBar1.Value = Math.Min(progressBar1.Maximum, done);
                                    labelStatus.Text = $"Готово {done}/{computers.Count}";
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
                    dataGridView1.Rows.Add(r.Computer, r.Status, r.MemberType, r.Account, r.Source, r.ExpandedFrom);
                }

                labelStatus.Text = $"Готово: {computers.Count} ПК. Ошибок: {errors.Count}.";
            }
            catch (OperationCanceledException)
            {
                labelStatus.Text = "Остановлено пользователем.";
            }
            finally
            {
                buttonScan.Enabled = true;
                buttonStop.Enabled = false;
                buttonLoadFromAD.Enabled = true;
                UseWaitCursor = false;
                cts?.Dispose();
                cts = null;
            }
        }

        void ExportCsv()
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new StringBuilder();
            // Заголовок
            sb.AppendLine("Computer;Status;MemberType;Account;Source;ExpandedFrom");

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string[] cells = new string[dataGridView1.Columns.Count];
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
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
    }
}
