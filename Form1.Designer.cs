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
            this.textBoxComputers = new System.Windows.Forms.TextBox();
            this.buttonLoadFromAD = new System.Windows.Forms.Button();
            this.buttonScan = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.checkBoxExpandGroups = new System.Windows.Forms.CheckBox();
            this.numericUpDownThreads = new System.Windows.Forms.NumericUpDown();
            this.buttonExport = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.labelStatus = new System.Windows.Forms.Label();
            this.labelThreads = new System.Windows.Forms.Label();
            this.groupBoxComputers = new System.Windows.Forms.GroupBox();
            this.groupBoxControls = new System.Windows.Forms.GroupBox();
            this.groupBoxResults = new System.Windows.Forms.GroupBox();
            this.labelAddMembers = new System.Windows.Forms.Label();
            this.textBoxAddMembers = new System.Windows.Forms.TextBox();
            this.buttonAddToSelected = new System.Windows.Forms.Button();
            this.buttonAddToAll = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreads)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBoxComputers.SuspendLayout();
            this.groupBoxControls.SuspendLayout();
            this.groupBoxResults.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBoxComputers
            // 
            this.textBoxComputers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxComputers.Location = new System.Drawing.Point(6, 22);
            this.textBoxComputers.Multiline = true;
            this.textBoxComputers.Name = "textBoxComputers";
            this.textBoxComputers.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxComputers.Size = new System.Drawing.Size(340, 480);
            this.textBoxComputers.TabIndex = 0;
            // 
            // buttonLoadFromAD
            // 
            this.buttonLoadFromAD.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonLoadFromAD.Location = new System.Drawing.Point(6, 508);
            this.buttonLoadFromAD.Name = "buttonLoadFromAD";
            this.buttonLoadFromAD.Size = new System.Drawing.Size(160, 30);
            this.buttonLoadFromAD.TabIndex = 1;
            this.buttonLoadFromAD.Text = "Загрузить ПК из AD";
            this.buttonLoadFromAD.UseVisualStyleBackColor = true;
            // 
            // buttonScan
            // 
            this.buttonScan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonScan.Location = new System.Drawing.Point(172, 508);
            this.buttonScan.Name = "buttonScan";
            this.buttonScan.Size = new System.Drawing.Size(100, 30);
            this.buttonScan.TabIndex = 2;
            this.buttonScan.Text = "Сканировать";
            this.buttonScan.UseVisualStyleBackColor = true;
            // 
            // buttonStop
            // 
            this.buttonStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStop.Location = new System.Drawing.Point(278, 508);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(68, 30);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.Text = "Стоп";
            this.buttonStop.UseVisualStyleBackColor = true;
            // 
            // checkBoxExpandGroups
            // 
            this.checkBoxExpandGroups.AutoSize = true;
            this.checkBoxExpandGroups.Location = new System.Drawing.Point(6, 22);
            this.checkBoxExpandGroups.Name = "checkBoxExpandGroups";
            this.checkBoxExpandGroups.Size = new System.Drawing.Size(224, 19);
            this.checkBoxExpandGroups.TabIndex = 4;
            this.checkBoxExpandGroups.Text = "Разворачивать доменные группы";
            this.checkBoxExpandGroups.UseVisualStyleBackColor = true;
            // 
            // numericUpDownThreads
            // 
            this.numericUpDownThreads.Location = new System.Drawing.Point(154, 47);
            this.numericUpDownThreads.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.numericUpDownThreads.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownThreads.Name = "numericUpDownThreads";
            this.numericUpDownThreads.Size = new System.Drawing.Size(60, 23);
            this.numericUpDownThreads.TabIndex = 5;
            this.numericUpDownThreads.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // buttonExport
            // 
            this.buttonExport.Location = new System.Drawing.Point(236, 47);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(100, 23);
            this.buttonExport.TabIndex = 6;
            this.buttonExport.Text = "Экспорт CSV";
            this.buttonExport.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(6, 22);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 25;
            this.dataGridView1.Size = new System.Drawing.Size(695, 480);
            this.dataGridView1.TabIndex = 7;
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(12, 620);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1060, 20);
            this.progressBar1.TabIndex = 8;
            // 
            // labelStatus
            // 
            this.labelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelStatus.Location = new System.Drawing.Point(12, 646);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(1060, 20);
            this.labelStatus.TabIndex = 9;
            this.labelStatus.Text = "Готов к работе";
            // 
            // labelThreads
            // 
            this.labelThreads.AutoSize = true;
            this.labelThreads.Location = new System.Drawing.Point(6, 49);
            this.labelThreads.Name = "labelThreads";
            this.labelThreads.Size = new System.Drawing.Size(142, 15);
            this.labelThreads.TabIndex = 10;
            this.labelThreads.Text = "Параллельных потоков:";
            // 
            // groupBoxComputers
            // 
            this.groupBoxComputers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxComputers.Controls.Add(this.textBoxComputers);
            this.groupBoxComputers.Controls.Add(this.buttonLoadFromAD);
            this.groupBoxComputers.Controls.Add(this.buttonScan);
            this.groupBoxComputers.Controls.Add(this.buttonStop);
            this.groupBoxComputers.Location = new System.Drawing.Point(12, 12);
            this.groupBoxComputers.Name = "groupBoxComputers";
            this.groupBoxComputers.Size = new System.Drawing.Size(352, 544);
            this.groupBoxComputers.TabIndex = 11;
            this.groupBoxComputers.TabStop = false;
            this.groupBoxComputers.Text = "Компьютеры для сканирования";
            // 
            // groupBoxControls
            // 
            this.groupBoxControls.Controls.Add(this.checkBoxExpandGroups);
            this.groupBoxControls.Controls.Add(this.labelThreads);
            this.groupBoxControls.Controls.Add(this.numericUpDownThreads);
            this.groupBoxControls.Controls.Add(this.buttonExport);
            this.groupBoxControls.Location = new System.Drawing.Point(370, 12);
            this.groupBoxControls.Name = "groupBoxControls";
            this.groupBoxControls.Size = new System.Drawing.Size(342, 76);
            this.groupBoxControls.TabIndex = 12;
            this.groupBoxControls.TabStop = false;
            this.groupBoxControls.Text = "Параметры сканирования";
            // 
            // groupBoxResults
            // 
            this.groupBoxResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxResults.Controls.Add(this.dataGridView1);
            this.groupBoxResults.Location = new System.Drawing.Point(370, 94);
            this.groupBoxResults.Name = "groupBoxResults";
            this.groupBoxResults.Size = new System.Drawing.Size(707, 508);
            this.groupBoxResults.TabIndex = 13;
            this.groupBoxResults.TabStop = false;
            this.groupBoxResults.Text = "Результаты сканирования";
            // 
            // labelAddMembers
            // 
            this.labelAddMembers.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelAddMembers.AutoSize = true;
            this.labelAddMembers.Location = new System.Drawing.Point(20, 615);
            this.labelAddMembers.Name = "labelAddMembers";
            this.labelAddMembers.Size = new System.Drawing.Size(63, 15);
            this.labelAddMembers.TabIndex = 14;
            this.labelAddMembers.Text = "Добавить:";
            // 
            // textBoxAddMembers
            // 
            this.textBoxAddMembers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxAddMembers.Location = new System.Drawing.Point(90, 612);
            this.textBoxAddMembers.Name = "textBoxAddMembers";
            this.textBoxAddMembers.PlaceholderText = "DOMAIN\\user; DOMAIN\\group; PCNAME\\localuser";
            this.textBoxAddMembers.Size = new System.Drawing.Size(650, 23);
            this.textBoxAddMembers.TabIndex = 15;
            // 
            // buttonAddToSelected
            // 
            this.buttonAddToSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddToSelected.Location = new System.Drawing.Point(750, 610);
            this.buttonAddToSelected.Name = "buttonAddToSelected";
            this.buttonAddToSelected.Size = new System.Drawing.Size(150, 27);
            this.buttonAddToSelected.TabIndex = 16;
            this.buttonAddToSelected.Text = "На выбранные ПК";
            this.buttonAddToSelected.UseVisualStyleBackColor = true;
            // 
            // buttonAddToAll
            // 
            this.buttonAddToAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddToAll.Location = new System.Drawing.Point(910, 610);
            this.buttonAddToAll.Name = "buttonAddToAll";
            this.buttonAddToAll.Size = new System.Drawing.Size(100, 27);
            this.buttonAddToAll.TabIndex = 17;
            this.buttonAddToAll.Text = "На все ПК";
            this.buttonAddToAll.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 681);
            this.Controls.Add(this.buttonAddToAll);
            this.Controls.Add(this.buttonAddToSelected);
            this.Controls.Add(this.textBoxAddMembers);
            this.Controls.Add(this.labelAddMembers);
            this.Controls.Add(this.groupBoxResults);
            this.Controls.Add(this.groupBoxControls);
            this.Controls.Add(this.groupBoxComputers);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.progressBar1);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "Form1";
            this.Text = "Сканер локальных администраторов";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreads)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBoxComputers.ResumeLayout(false);
            this.groupBoxComputers.PerformLayout();
            this.groupBoxControls.ResumeLayout(false);
            this.groupBoxControls.PerformLayout();
            this.groupBoxResults.ResumeLayout(false);
            this.ResumeLayout(false);

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
