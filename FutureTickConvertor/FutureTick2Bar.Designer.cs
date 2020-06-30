namespace FutureTickConvertor
{
    partial class FutureTick2Bar
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
            this.btnDeleteProgMQ = new System.Windows.Forms.Button();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.txtInfo = new System.Windows.Forms.RichTextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.txtTargetConfig = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.chkFixBar = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.chkStartDate = new System.Windows.Forms.DateTimePicker();
            this.txtTickDir = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtTargetDay = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtTargetMin = new System.Windows.Forms.TextBox();
            this.myTabs = new System.Windows.Forms.TabControl();
            this.maingpb = new System.Windows.Forms.GroupBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.myTabs.SuspendLayout();
            this.maingpb.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnDeleteProgMQ
            // 
            this.btnDeleteProgMQ.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDeleteProgMQ.Location = new System.Drawing.Point(215, 276);
            this.btnDeleteProgMQ.Name = "btnDeleteProgMQ";
            this.btnDeleteProgMQ.Size = new System.Drawing.Size(128, 25);
            this.btnDeleteProgMQ.TabIndex = 21;
            this.btnDeleteProgMQ.Text = "清空历史转换记录";
            this.btnDeleteProgMQ.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.txtInfo);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1170, 642);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "日志";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // txtInfo
            // 
            this.txtInfo.BackColor = System.Drawing.Color.Black;
            this.txtInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInfo.ForeColor = System.Drawing.Color.Lime;
            this.txtInfo.Location = new System.Drawing.Point(3, 3);
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.ReadOnly = true;
            this.txtInfo.Size = new System.Drawing.Size(1164, 636);
            this.txtInfo.TabIndex = 9;
            this.txtInfo.Text = "";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.txtTargetConfig);
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.btnDeleteProgMQ);
            this.tabPage2.Controls.Add(this.chkFixBar);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.chkStartDate);
            this.tabPage2.Controls.Add(this.txtTickDir);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.txtTargetDay);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.txtTargetMin);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1170, 642);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "选项";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // txtTargetConfig
            // 
            this.txtTargetConfig.Location = new System.Drawing.Point(115, 204);
            this.txtTargetConfig.Name = "txtTargetConfig";
            this.txtTargetConfig.Size = new System.Drawing.Size(335, 21);
            this.txtTargetConfig.TabIndex = 24;
            this.txtTargetConfig.Text = "D:\\WorkSpace\\HawuQuant\\QuantFactory\\WorkSpace\\FutureTickConvertor\\MqTestData\\Conf" +
    "ig";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(19, 206);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 12);
            this.label7.TabIndex = 23;
            this.label7.Text = "配置目录";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 240);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(197, 12);
            this.label6.TabIndex = 22;
            this.label6.Text = "MQ会自动根据历史转换记录增量转换";
            // 
            // chkFixBar
            // 
            this.chkFixBar.AutoSize = true;
            this.chkFixBar.Checked = true;
            this.chkFixBar.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFixBar.Location = new System.Drawing.Point(20, 27);
            this.chkFixBar.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.chkFixBar.Name = "chkFixBar";
            this.chkFixBar.Size = new System.Drawing.Size(420, 16);
            this.chkFixBar.TabIndex = 21;
            this.chkFixBar.Text = "基于TradingFrame文件补全由于成交清淡导致没有Tick行情时间段的分钟线";
            this.chkFixBar.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 282);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(191, 12);
            this.label5.TabIndex = 20;
            this.label5.Text = "如果需要强制从某天开始转换,请先";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 74);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 12);
            this.label4.TabIndex = 14;
            this.label4.Text = "Tick目录";
            // 
            // chkStartDate
            // 
            this.chkStartDate.Location = new System.Drawing.Point(491, 280);
            this.chkStartDate.Margin = new System.Windows.Forms.Padding(2);
            this.chkStartDate.Name = "chkStartDate";
            this.chkStartDate.Size = new System.Drawing.Size(111, 21);
            this.chkStartDate.TabIndex = 19;
            this.chkStartDate.Value = new System.DateTime(2010, 1, 1, 0, 0, 0, 0);
            // 
            // txtTickDir
            // 
            this.txtTickDir.Location = new System.Drawing.Point(114, 71);
            this.txtTickDir.Name = "txtTickDir";
            this.txtTickDir.Size = new System.Drawing.Size(335, 21);
            this.txtTickDir.TabIndex = 15;
            this.txtTickDir.Text = "D:\\WorkSpace\\HawuQuant\\QuantFactory\\WorkSpace\\FutureTickConvertor\\MqTestData\\futu" +
    "retick";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(355, 282);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(131, 12);
            this.label3.TabIndex = 17;
            this.label3.Text = "然后设置开始转换日期:";
            // 
            // txtTargetDay
            // 
            this.txtTargetDay.Location = new System.Drawing.Point(114, 166);
            this.txtTargetDay.Name = "txtTargetDay";
            this.txtTargetDay.Size = new System.Drawing.Size(335, 21);
            this.txtTargetDay.TabIndex = 20;
            this.txtTargetDay.Text = "D:\\WorkSpace\\HawuQuant\\QuantFactory\\WorkSpace\\FutureTickConvertor\\MqTestData\\futu" +
    "reday";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 122);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 12);
            this.label1.TabIndex = 17;
            this.label1.Text = "分钟线目录";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 169);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 19;
            this.label2.Text = "日线目录";
            // 
            // txtTargetMin
            // 
            this.txtTargetMin.Location = new System.Drawing.Point(114, 119);
            this.txtTargetMin.Name = "txtTargetMin";
            this.txtTargetMin.Size = new System.Drawing.Size(335, 21);
            this.txtTargetMin.TabIndex = 18;
            this.txtTargetMin.Text = "D:\\WorkSpace\\HawuQuant\\QuantFactory\\WorkSpace\\FutureTickConvertor\\MqTestData\\futu" +
    "remin";
            // 
            // myTabs
            // 
            this.myTabs.Controls.Add(this.tabPage1);
            this.myTabs.Controls.Add(this.tabPage2);
            this.myTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myTabs.Location = new System.Drawing.Point(3, 78);
            this.myTabs.Name = "myTabs";
            this.myTabs.SelectedIndex = 0;
            this.myTabs.Size = new System.Drawing.Size(1178, 668);
            this.myTabs.TabIndex = 1;
            // 
            // maingpb
            // 
            this.maingpb.Controls.Add(this.btnStart);
            this.maingpb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.maingpb.Location = new System.Drawing.Point(3, 3);
            this.maingpb.Name = "maingpb";
            this.maingpb.Size = new System.Drawing.Size(1178, 69);
            this.maingpb.TabIndex = 0;
            this.maingpb.TabStop = false;
            // 
            // btnStart
            // 
            this.btnStart.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnStart.Location = new System.Drawing.Point(9, 24);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(78, 25);
            this.btnStart.TabIndex = 5;
            this.btnStart.Text = "开始转换";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.maingpb, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.myTabs, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1184, 749);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // FutureTick2Bar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 749);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(1);
            this.Name = "FutureTick2Bar";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "期货Tick转K线";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.myTabs.ResumeLayout(false);
            this.maingpb.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnDeleteProgMQ;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.RichTextBox txtInfo;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkFixBar;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker chkStartDate;
        private System.Windows.Forms.TextBox txtTickDir;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtTargetDay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtTargetMin;
        private System.Windows.Forms.TabControl myTabs;
        private System.Windows.Forms.GroupBox maingpb;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox txtTargetConfig;
        private System.Windows.Forms.Label label7;
    }
}