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
        private TextBox? txtAddAccounts;
        private Button? btnAddToSelected, btnAddToAll;

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
            
            // Кнопка-колонка "Удалить" в начале таблицы
            var colDel = new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "Действие",
                Text = "Удалить",
                UseColumnTextForButtonValue = true,
                Width = 90
            };
            dataGridView1.Columns.Add(colDel);
            
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

            // Панель добавления аккаунтов
            var lblAdd = new Label { 
                Location = new Point(20, 615), 
                Size = new Size(80, 23), 
                Text = "Добавить:",
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            txtAddAccounts = new TextBox { 
                Location = new Point(100, 612), 
                Size = new Size(650, 23), 
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                PlaceholderText = "DOMAIN\\user; DOMAIN\\group; PCNAME\\localuser"
            };
            btnAddToSelected = new Button { 
                Location = new Point(760, 610), 
                Size = new Size(150, 27), 
                Text = "На выбранные ПК", 
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right 
            };
            btnAddToAll = new Button { 
                Location = new Point(920, 610), 
                Size = new Size(100, 27), 
                Text = "На все ПК", 
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right 
            };

            // Привязка событий
            buttonLoadFromAD.Click += async (s, e) => await LoadComputersFromAD();
            buttonScan.Click += async (s, e) => await ScanAsync();
            buttonStop.Click += (s, e) => cts?.Cancel();
            buttonExport.Click += (s, e) => ExportCsv();
            
            // События управления членством
            dataGridView1.CellContentClick += Grid_CellContentClick;
            btnAddToSelected.Click += async (s, e) => await AddMembersAsync("selected");
            btnAddToAll.Click += async (s, e) => await AddMembersAsync("all");

            // Настройка статус-бара
            labelStatus.Text = "Готов к работе";

            // Добавляем элементы управления для добавления/удаления в конце
            // чтобы они были поверх GroupBox контролов
            Controls.Add(lblAdd);
            Controls.Add(txtAddAccounts);
            Controls.Add(btnAddToSelected);
            Controls.Add(btnAddToAll);
            
            // Поднимаем их на передний план
            lblAdd.BringToFront();
            txtAddAccounts?.BringToFront();
            btnAddToSelected?.BringToFront();
            btnAddToAll?.BringToFront();
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
                    dataGridView1.Rows.Add("Удалить", r.Computer, r.Status, r.MemberType, r.Account, r.Source, r.ExpandedFrom);
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
            // Заголовок (пропускаем колонку кнопки)
            var headerCols = dataGridView1.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Name != "colDelete")
                .Select(c => c.HeaderText);
            sb.AppendLine(string.Join(";", headerCols));

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var cells = new List<string>();
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    if (dataGridView1.Columns[i].Name == "colDelete") continue; // пропускаем колонку кнопки
                    var val = row.Cells[i].Value?.ToString() ?? "";
                    // Экраним ; и "
                    if (val.Contains(";") || val.Contains("\""))
                        val = "\"" + val.Replace("\"", "\"\"") + "\"";
                    cells.Add(val);
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

        async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name != "colDelete") return;

            var row = dataGridView1.Rows[e.RowIndex];
            string computer = row.Cells["Computer"].Value?.ToString() ?? "";
            string source = row.Cells["Source"].Value?.ToString() ?? "";
            string expanded = row.Cells["ExpandedFrom"].Value?.ToString() ?? "";
            string account = row.Cells["Account"].Value?.ToString() ?? "";

            // Удаляем ТОЛЬКО прямых членов из NetAPI32 (не развёрнутых из доменных групп)
            if (source != "NetAPI32" || !string.IsNullOrEmpty(expanded) || string.IsNullOrWhiteSpace(account))
            {
                MessageBox.Show("Удалять можно только прямых членов локальной группы (Источник=NetAPI32, 'Развёрнуто из' — пусто).", "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить '{account}' из локальной группы Администраторы на {computer}?",
                "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            UseWaitCursor = true;
            row.Cells["Status"].Value = "Удаление…";

            try
            {
                await Task.Run(() => LocalAdminsReader.RemoveLocalAdmin(computer, account));
                // если успешно — можно удалить строку из гриды или перезагрузить ПК
                dataGridView1.Rows.RemoveAt(e.RowIndex);
            }
            catch (Exception ex)
            {
                row.Cells["Status"].Value = "Ошибка: " + ex.Message;
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        async Task AddMembersAsync(string target)
        {
            // список аккаунтов из текстбокса
            var accounts = txtAddAccounts?.Text?
                .Split(new[] { ';', ',', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Contains("\\"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (accounts.Count == 0)
            {
                MessageBox.Show("Укажи аккаунты вида DOMAIN\\User; DOMAIN\\Group; COMPUTER\\LocalUser", "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // целевые компьютеры
            List<string> computers;
            if (target == "selected")
            {
                // ПК из выделенных строк таблицы
                var fromGrid = dataGridView1.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(r => r.Cells["Computer"].Value?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (fromGrid.Count == 0)
                {
                    MessageBox.Show("Выдели строки таблицы (хотя бы по одной на ПК), или используй 'Добавить на все ПК'.", "Нет выбранных ПК", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                computers = fromGrid!;
            }
            else
            {
                // все ПК из левого списка/бокса
                computers = textBoxComputers.Text
                    .Split(new[] { '\r', '\n', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (computers.Count == 0)
            {
                MessageBox.Show("Список ПК пуст.", "Нет ПК", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UseWaitCursor = true;
            if (btnAddToSelected != null) btnAddToSelected.Enabled = false;
            if (btnAddToAll != null) btnAddToAll.Enabled = false;
            labelStatus.Text = $"Добавление на {computers.Count} ПК…";
            progressBar1.Value = 0; 
            progressBar1.Maximum = computers.Count;

            var dop = (int)numericUpDownThreads.Value;
            var errors = new ConcurrentBag<string>();
            int done = 0;

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(computers, new ParallelOptions { MaxDegreeOfParallelism = dop }, pc =>
                    {
                        try
                        {
                            foreach (var acc in accounts) LocalAdminsReader.AddLocalAdmin(pc, acc);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{pc}: {ex.Message}");
                        }
                        finally
                        {
                            Interlocked.Increment(ref done);
                            Invoke(new Action(() =>
                            {
                                progressBar1.Value = done;
                                labelStatus.Text = $"Добавление: {done}/{computers.Count}";
                            }));
                        }
                    });
                });

                if (errors.Count == 0)
                    MessageBox.Show("Готово без ошибок.", "Добавление", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("Часть ПК с ошибками:\n" + string.Join("\n", errors.Take(20)) + (errors.Count > 20 ? $"\n… и ещё {errors.Count - 20}" : ""), "Добавление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (btnAddToSelected != null) btnAddToSelected.Enabled = true;
                if (btnAddToAll != null) btnAddToAll.Enabled = true;
                UseWaitCursor = false;
            }
        }
    }
}
