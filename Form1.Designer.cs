namespace who_admin
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBoxComputers = new TextBox();
            buttonLoadFromAD = new Button();
            buttonScan = new Button();
            buttonStop = new Button();
            checkBoxExpandGroups = new CheckBox();
            numericUpDownThreads = new NumericUpDown();
            buttonExport = new Button();
            dataGridView1 = new DataGridView();
            progressBar1 = new ProgressBar();
            labelStatus = new Label();
            labelThreads = new Label();
            groupBoxComputers = new GroupBox();
            groupBoxControls = new GroupBox();
            groupBoxResults = new GroupBox();
            buttonAddToAll = new Button();
            buttonAddToSelected = new Button();
            textBoxAddMembers = new TextBox();
            labelAddMembers = new Label();
            ((System.ComponentModel.ISupportInitialize)numericUpDownThreads).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            groupBoxComputers.SuspendLayout();
            groupBoxControls.SuspendLayout();
            groupBoxResults.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxComputers
            // 
            textBoxComputers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            textBoxComputers.Location = new Point(6, 22);
            textBoxComputers.Multiline = true;
            textBoxComputers.Name = "textBoxComputers";
            textBoxComputers.ScrollBars = ScrollBars.Vertical;
            textBoxComputers.Size = new Size(340, 661);
            textBoxComputers.TabIndex = 0;
            // 
            // buttonLoadFromAD
            // 
            buttonLoadFromAD.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonLoadFromAD.Location = new Point(6, 689);
            buttonLoadFromAD.Name = "buttonLoadFromAD";
            buttonLoadFromAD.Size = new Size(160, 30);
            buttonLoadFromAD.TabIndex = 1;
            buttonLoadFromAD.Text = "Загрузить ПК из AD";
            buttonLoadFromAD.UseVisualStyleBackColor = true;
            // 
            // buttonScan
            // 
            buttonScan.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonScan.Location = new Point(172, 689);
            buttonScan.Name = "buttonScan";
            buttonScan.Size = new Size(100, 30);
            buttonScan.TabIndex = 2;
            buttonScan.Text = "Сканировать";
            buttonScan.UseVisualStyleBackColor = true;
            // 
            // buttonStop
            // 
            buttonStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStop.Location = new Point(278, 689);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(68, 30);
            buttonStop.TabIndex = 3;
            buttonStop.Text = "Стоп";
            buttonStop.UseVisualStyleBackColor = true;
            // 
            // checkBoxExpandGroups
            // 
            checkBoxExpandGroups.AutoSize = true;
            checkBoxExpandGroups.Location = new Point(6, 22);
            checkBoxExpandGroups.Name = "checkBoxExpandGroups";
            checkBoxExpandGroups.Size = new Size(211, 19);
            checkBoxExpandGroups.TabIndex = 4;
            checkBoxExpandGroups.Text = "Разворачивать доменные группы";
            checkBoxExpandGroups.UseVisualStyleBackColor = true;
            // 
            // numericUpDownThreads
            // 
            numericUpDownThreads.Location = new Point(154, 47);
            numericUpDownThreads.Maximum = new decimal(new int[] { 128, 0, 0, 0 });
            numericUpDownThreads.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDownThreads.Name = "numericUpDownThreads";
            numericUpDownThreads.Size = new Size(60, 23);
            numericUpDownThreads.TabIndex = 5;
            numericUpDownThreads.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // buttonExport
            // 
            buttonExport.Location = new Point(236, 47);
            buttonExport.Name = "buttonExport";
            buttonExport.Size = new Size(100, 23);
            buttonExport.TabIndex = 6;
            buttonExport.Text = "Экспорт CSV";
            buttonExport.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(6, 22);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(1081, 621);
            dataGridView1.TabIndex = 7;
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(12, 801);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(1446, 20);
            progressBar1.TabIndex = 8;
            // 
            // labelStatus
            // 
            labelStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelStatus.Location = new Point(11, 833);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(1446, 20);
            labelStatus.TabIndex = 9;
            labelStatus.Text = "Готов к работе";
            // 
            // labelThreads
            // 
            labelThreads.AutoSize = true;
            labelThreads.Location = new Point(6, 49);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(141, 15);
            labelThreads.TabIndex = 10;
            labelThreads.Text = "Параллельных потоков:";
            // 
            // groupBoxComputers
            // 
            groupBoxComputers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBoxComputers.Controls.Add(textBoxComputers);
            groupBoxComputers.Controls.Add(buttonLoadFromAD);
            groupBoxComputers.Controls.Add(buttonScan);
            groupBoxComputers.Controls.Add(buttonStop);
            groupBoxComputers.Location = new Point(12, 12);
            groupBoxComputers.Name = "groupBoxComputers";
            groupBoxComputers.Size = new Size(352, 725);
            groupBoxComputers.TabIndex = 11;
            groupBoxComputers.TabStop = false;
            groupBoxComputers.Text = "Компьютеры для сканирования";
            // 
            // groupBoxControls
            // 
            groupBoxControls.Controls.Add(checkBoxExpandGroups);
            groupBoxControls.Controls.Add(labelThreads);
            groupBoxControls.Controls.Add(numericUpDownThreads);
            groupBoxControls.Controls.Add(buttonExport);
            groupBoxControls.Location = new Point(370, 12);
            groupBoxControls.Name = "groupBoxControls";
            groupBoxControls.Size = new Size(342, 76);
            groupBoxControls.TabIndex = 12;
            groupBoxControls.TabStop = false;
            groupBoxControls.Text = "Параметры сканирования";
            // 
            // groupBoxResults
            // 
            groupBoxResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxResults.Controls.Add(buttonAddToAll);
            groupBoxResults.Controls.Add(dataGridView1);
            groupBoxResults.Controls.Add(buttonAddToSelected);
            groupBoxResults.Controls.Add(textBoxAddMembers);
            groupBoxResults.Location = new Point(370, 94);
            groupBoxResults.Name = "groupBoxResults";
            groupBoxResults.Size = new Size(1093, 689);
            groupBoxResults.TabIndex = 13;
            groupBoxResults.TabStop = false;
            groupBoxResults.Text = "Результаты сканирования";
            // 
            // buttonAddToAll
            // 
            buttonAddToAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAddToAll.Location = new Point(924, 661);
            buttonAddToAll.Name = "buttonAddToAll";
            buttonAddToAll.Size = new Size(100, 27);
            buttonAddToAll.TabIndex = 17;
            buttonAddToAll.Text = "На все ПК";
            buttonAddToAll.UseVisualStyleBackColor = true;
            // 
            // buttonAddToSelected
            // 
            buttonAddToSelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAddToSelected.Location = new Point(768, 662);
            buttonAddToSelected.Name = "buttonAddToSelected";
            buttonAddToSelected.Size = new Size(150, 27);
            buttonAddToSelected.TabIndex = 16;
            buttonAddToSelected.Text = "На выбранные ПК";
            buttonAddToSelected.UseVisualStyleBackColor = true;
            // 
            // textBoxAddMembers
            // 
            textBoxAddMembers.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxAddMembers.Location = new Point(6, 664);
            textBoxAddMembers.Name = "textBoxAddMembers";
            textBoxAddMembers.PlaceholderText = "DOMAIN\\user; DOMAIN\\group; PCNAME\\localuser";
            textBoxAddMembers.Size = new Size(756, 23);
            textBoxAddMembers.TabIndex = 15;
            // 
            // labelAddMembers
            // 
            labelAddMembers.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelAddMembers.AutoSize = true;
            labelAddMembers.Location = new Point(290, 767);
            labelAddMembers.Name = "labelAddMembers";
            labelAddMembers.Size = new Size(62, 15);
            labelAddMembers.TabIndex = 14;
            labelAddMembers.Text = "Добавить:";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1470, 862);
            Controls.Add(labelAddMembers);
            Controls.Add(groupBoxResults);
            Controls.Add(groupBoxControls);
            Controls.Add(groupBoxComputers);
            Controls.Add(labelStatus);
            Controls.Add(progressBar1);
            MinimumSize = new Size(900, 600);
            Name = "Form1";
            Text = "Сканер локальных администраторов";
            ((System.ComponentModel.ISupportInitialize)numericUpDownThreads).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            groupBoxComputers.ResumeLayout(false);
            groupBoxComputers.PerformLayout();
            groupBoxControls.ResumeLayout(false);
            groupBoxControls.PerformLayout();
            groupBoxResults.ResumeLayout(false);
            groupBoxResults.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private TextBox textBoxComputers;
        private Button buttonLoadFromAD;
        private Button buttonScan;
        private Button buttonStop;
        private CheckBox checkBoxExpandGroups;
        private NumericUpDown numericUpDownThreads;
        private Button buttonExport;
        private DataGridView dataGridView1;
        private ProgressBar progressBar1;
        private Label labelStatus;
        private Label labelThreads;
        private GroupBox groupBoxComputers;
        private GroupBox groupBoxControls;
        private GroupBox groupBoxResults;
        private Label labelAddMembers;
        private TextBox textBoxAddMembers;
        private Button buttonAddToSelected;
        private Button buttonAddToAll;
    }
}
