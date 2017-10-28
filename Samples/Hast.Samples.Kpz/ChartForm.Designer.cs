namespace Hast.Samples.Kpz
{
    partial class ChartForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea4 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend4 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.chartKPZ = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panelTop = new System.Windows.Forms.Panel();
            this.checkRandomSeed = new System.Windows.Forms.CheckBox();
            this.labelRandomSeed = new System.Windows.Forms.Label();
            this.checkWriteToFile = new System.Windows.Forms.CheckBox();
            this.labelWriteToFile = new System.Windows.Forms.Label();
            this.checkVerifyOutput = new System.Windows.Forms.CheckBox();
            this.labelVerifyOutput = new System.Windows.Forms.Label();
            this.labelStepByStep = new System.Windows.Forms.Label();
            this.checkStep = new System.Windows.Forms.CheckBox();
            this.labelShowInspector = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboTarget = new System.Windows.Forms.ComboBox();
            this.checkShowInspector = new System.Windows.Forms.CheckBox();
            this.nudTableHeight = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.nudTableWidth = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.buttonStart = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listLog = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.chartKPZ)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTableHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTableWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chartKPZ
            // 
            chartArea4.AxisY.IsLogarithmic = true;
            chartArea4.Name = "ChartArea1";
            this.chartKPZ.ChartAreas.Add(chartArea4);
            this.chartKPZ.Dock = System.Windows.Forms.DockStyle.Fill;
            legend4.Name = "Legend1";
            this.chartKPZ.Legends.Add(legend4);
            this.chartKPZ.Location = new System.Drawing.Point(0, 0);
            this.chartKPZ.Name = "chartKPZ";
            series4.ChartArea = "ChartArea1";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series4.IsVisibleInLegend = false;
            series4.Legend = "Legend1";
            series4.Name = "defaultSeries";
            this.chartKPZ.Series.Add(series4);
            this.chartKPZ.Size = new System.Drawing.Size(617, 219);
            this.chartKPZ.TabIndex = 0;
            this.chartKPZ.Text = "chart1";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel1.Controls.Add(this.panelTop, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.progressBar, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonStart, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 165F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(623, 636);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // panelTop
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panelTop, 2);
            this.panelTop.Controls.Add(this.checkRandomSeed);
            this.panelTop.Controls.Add(this.labelRandomSeed);
            this.panelTop.Controls.Add(this.checkWriteToFile);
            this.panelTop.Controls.Add(this.labelWriteToFile);
            this.panelTop.Controls.Add(this.checkVerifyOutput);
            this.panelTop.Controls.Add(this.labelVerifyOutput);
            this.panelTop.Controls.Add(this.labelStepByStep);
            this.panelTop.Controls.Add(this.checkStep);
            this.panelTop.Controls.Add(this.labelShowInspector);
            this.panelTop.Controls.Add(this.label4);
            this.panelTop.Controls.Add(this.comboTarget);
            this.panelTop.Controls.Add(this.checkShowInspector);
            this.panelTop.Controls.Add(this.nudTableHeight);
            this.panelTop.Controls.Add(this.label3);
            this.panelTop.Controls.Add(this.nudTableWidth);
            this.panelTop.Controls.Add(this.label2);
            this.panelTop.Controls.Add(this.label1);
            this.panelTop.Controls.Add(this.nudIterations);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTop.Location = new System.Drawing.Point(3, 3);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(617, 159);
            this.panelTop.TabIndex = 4;
            // 
            // checkRandomSeed
            // 
            this.checkRandomSeed.AutoSize = true;
            this.checkRandomSeed.Location = new System.Drawing.Point(595, 54);
            this.checkRandomSeed.Name = "checkRandomSeed";
            this.checkRandomSeed.Size = new System.Drawing.Size(15, 14);
            this.checkRandomSeed.TabIndex = 18;
            this.checkRandomSeed.UseVisualStyleBackColor = true;
            // 
            // labelRandomSeed
            // 
            this.labelRandomSeed.AutoSize = true;
            this.labelRandomSeed.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelRandomSeed.Location = new System.Drawing.Point(470, 47);
            this.labelRandomSeed.Name = "labelRandomSeed";
            this.labelRandomSeed.Size = new System.Drawing.Size(119, 23);
            this.labelRandomSeed.TabIndex = 17;
            this.labelRandomSeed.Text = "Random seed:";
            this.labelRandomSeed.Click += new System.EventHandler(this.labelRandomSeed_Click);
            // 
            // checkWriteToFile
            // 
            this.checkWriteToFile.AutoSize = true;
            this.checkWriteToFile.Location = new System.Drawing.Point(595, 15);
            this.checkWriteToFile.Name = "checkWriteToFile";
            this.checkWriteToFile.Size = new System.Drawing.Size(15, 14);
            this.checkWriteToFile.TabIndex = 16;
            this.checkWriteToFile.UseVisualStyleBackColor = true;
            // 
            // labelWriteToFile
            // 
            this.labelWriteToFile.AutoSize = true;
            this.labelWriteToFile.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelWriteToFile.Location = new System.Drawing.Point(470, 8);
            this.labelWriteToFile.Name = "labelWriteToFile";
            this.labelWriteToFile.Size = new System.Drawing.Size(104, 23);
            this.labelWriteToFile.TabIndex = 15;
            this.labelWriteToFile.Text = "Write to file:";
            this.labelWriteToFile.Click += new System.EventHandler(this.labelWriteToFile_Click);
            // 
            // checkVerifyOutput
            // 
            this.checkVerifyOutput.AutoSize = true;
            this.checkVerifyOutput.Location = new System.Drawing.Point(449, 93);
            this.checkVerifyOutput.Name = "checkVerifyOutput";
            this.checkVerifyOutput.Size = new System.Drawing.Size(15, 14);
            this.checkVerifyOutput.TabIndex = 14;
            this.checkVerifyOutput.UseVisualStyleBackColor = true;
            // 
            // labelVerifyOutput
            // 
            this.labelVerifyOutput.AutoSize = true;
            this.labelVerifyOutput.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelVerifyOutput.Location = new System.Drawing.Point(311, 86);
            this.labelVerifyOutput.Name = "labelVerifyOutput";
            this.labelVerifyOutput.Size = new System.Drawing.Size(132, 23);
            this.labelVerifyOutput.TabIndex = 13;
            this.labelVerifyOutput.Text = "Validate results:";
            this.labelVerifyOutput.Click += new System.EventHandler(this.labelVerifyOutput_Click);
            // 
            // labelStepByStep
            // 
            this.labelStepByStep.AutoSize = true;
            this.labelStepByStep.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStepByStep.Location = new System.Drawing.Point(311, 47);
            this.labelStepByStep.Name = "labelStepByStep";
            this.labelStepByStep.Size = new System.Drawing.Size(109, 23);
            this.labelStepByStep.TabIndex = 12;
            this.labelStepByStep.Text = "Step by step:";
            this.labelStepByStep.Click += new System.EventHandler(this.labelStepByStep_Click);
            // 
            // checkStep
            // 
            this.checkStep.AutoSize = true;
            this.checkStep.Location = new System.Drawing.Point(449, 54);
            this.checkStep.Name = "checkStep";
            this.checkStep.Size = new System.Drawing.Size(15, 14);
            this.checkStep.TabIndex = 11;
            this.checkStep.UseVisualStyleBackColor = true;
            // 
            // labelShowInspector
            // 
            this.labelShowInspector.AutoSize = true;
            this.labelShowInspector.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelShowInspector.Location = new System.Drawing.Point(309, 8);
            this.labelShowInspector.Name = "labelShowInspector";
            this.labelShowInspector.Size = new System.Drawing.Size(134, 23);
            this.labelShowInspector.TabIndex = 10;
            this.labelShowInspector.Text = "Show inspector:";
            this.labelShowInspector.Click += new System.EventHandler(this.labelShowInspector_Click_1);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label4.Location = new System.Drawing.Point(9, 125);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(134, 23);
            this.label4.TabIndex = 9;
            this.label4.Text = "Target platform:";
            // 
            // comboTarget
            // 
            this.comboTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTarget.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.comboTarget.FormattingEnabled = true;
            this.comboTarget.Items.AddRange(new object[] {
            "Original algorithm (CPU)",
            "Hastlayer simulation #1 (CPU)",
            "Hastlayer accelerated #1 (FPGA)",
            "Hastlayer simulation #2 (CPU) ",
            "Hastlayer simulation #2 (FPGA) ",
            "PRNG test (FPGA)"});
            this.comboTarget.Location = new System.Drawing.Point(202, 125);
            this.comboTarget.Name = "comboTarget";
            this.comboTarget.Size = new System.Drawing.Size(408, 31);
            this.comboTarget.TabIndex = 8;
            this.comboTarget.SelectedIndexChanged += new System.EventHandler(this.comboTarget_SelectedIndexChanged);
            // 
            // checkShowInspector
            // 
            this.checkShowInspector.AutoSize = true;
            this.checkShowInspector.Checked = true;
            this.checkShowInspector.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkShowInspector.Location = new System.Drawing.Point(449, 15);
            this.checkShowInspector.Name = "checkShowInspector";
            this.checkShowInspector.Size = new System.Drawing.Size(15, 14);
            this.checkShowInspector.TabIndex = 6;
            this.checkShowInspector.UseVisualStyleBackColor = true;
            // 
            // nudTableHeight
            // 
            this.nudTableHeight.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.nudTableHeight.Location = new System.Drawing.Point(202, 84);
            this.nudTableHeight.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.nudTableHeight.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.nudTableHeight.Name = "nudTableHeight";
            this.nudTableHeight.Size = new System.Drawing.Size(93, 31);
            this.nudTableHeight.TabIndex = 5;
            this.nudTableHeight.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label3.Location = new System.Drawing.Point(9, 86);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 23);
            this.label3.TabIndex = 4;
            this.label3.Text = "Table height:";
            // 
            // nudTableWidth
            // 
            this.nudTableWidth.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.nudTableWidth.Location = new System.Drawing.Point(202, 45);
            this.nudTableWidth.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.nudTableWidth.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.nudTableWidth.Name = "nudTableWidth";
            this.nudTableWidth.Size = new System.Drawing.Size(93, 31);
            this.nudTableWidth.TabIndex = 3;
            this.nudTableWidth.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.Location = new System.Drawing.Point(9, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 23);
            this.label2.TabIndex = 2;
            this.label2.Text = "Table width:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(9, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(162, 23);
            this.label1.TabIndex = 1;
            this.label1.Text = "KPZ iteration count:";
            // 
            // nudIterations
            // 
            this.nudIterations.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.nudIterations.Location = new System.Drawing.Point(202, 6);
            this.nudIterations.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.nudIterations.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(93, 31);
            this.nudIterations.TabIndex = 0;
            this.nudIterations.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.Location = new System.Drawing.Point(10, 175);
            this.progressBar.Margin = new System.Windows.Forms.Padding(10, 10, 0, 10);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(413, 36);
            this.progressBar.TabIndex = 1;
            // 
            // buttonStart
            // 
            this.buttonStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonStart.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonStart.Location = new System.Drawing.Point(433, 175);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(10);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(180, 36);
            this.buttonStart.TabIndex = 2;
            this.buttonStart.Text = "Start KPZ";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // splitContainer1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.splitContainer1, 2);
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 224);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listLog);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.chartKPZ);
            this.splitContainer1.Size = new System.Drawing.Size(617, 409);
            this.splitContainer1.SplitterDistance = 186;
            this.splitContainer1.TabIndex = 3;
            // 
            // listLog
            // 
            this.listLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listLog.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.listLog.FormattingEnabled = true;
            this.listLog.ItemHeight = 23;
            this.listLog.Location = new System.Drawing.Point(0, 0);
            this.listLog.Name = "listLog";
            this.listLog.Size = new System.Drawing.Size(617, 186);
            this.listLog.TabIndex = 0;
            // 
            // ChartForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(623, 636);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(639, 548);
            this.Name = "ChartForm";
            this.Text = "KPZ";
            ((System.ComponentModel.ISupportInitialize)(this.chartKPZ)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTableHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTableWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chartKPZ;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox listLog;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.CheckBox checkShowInspector;
        private System.Windows.Forms.NumericUpDown nudTableHeight;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudTableWidth;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboTarget;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkStep;
        private System.Windows.Forms.Label labelShowInspector;
        private System.Windows.Forms.Label labelStepByStep;
        private System.Windows.Forms.CheckBox checkVerifyOutput;
        private System.Windows.Forms.Label labelVerifyOutput;
        private System.Windows.Forms.CheckBox checkWriteToFile;
        private System.Windows.Forms.Label labelWriteToFile;
        private System.Windows.Forms.CheckBox checkRandomSeed;
        private System.Windows.Forms.Label labelRandomSeed;
    }
}
