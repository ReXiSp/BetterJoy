﻿namespace BetterJoy
{
    partial class MainForm
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
            if (disposing)
            {
                components?.Dispose();
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
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            console = new System.Windows.Forms.TextBox();
            notifyIcon = new System.Windows.Forms.NotifyIcon(components);
            contextMenu = new System.Windows.Forms.ContextMenuStrip(components);
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            version_lbl = new System.Windows.Forms.Label();
            donationLink = new System.Windows.Forms.LinkLabel();
            conCntrls = new System.Windows.Forms.GroupBox();
            loc4 = new System.Windows.Forms.Button();
            loc3 = new System.Windows.Forms.Button();
            loc2 = new System.Windows.Forms.Button();
            loc1 = new System.Windows.Forms.Button();
            con4 = new System.Windows.Forms.Button();
            con3 = new System.Windows.Forms.Button();
            con2 = new System.Windows.Forms.Button();
            con1 = new System.Windows.Forms.Button();
            btnTip = new System.Windows.Forms.ToolTip(components);
            foldLbl = new System.Windows.Forms.Label();
            startInTrayBox = new System.Windows.Forms.CheckBox();
            btn_open3rdP = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            settingsTable = new System.Windows.Forms.TableLayoutPanel();
            rightPanel = new System.Windows.Forms.Panel();
            settingsApply = new System.Windows.Forms.Button();
            AutoCalibrate = new System.Windows.Forms.Button();
            btn_reassign_open = new System.Windows.Forms.Button();
            contextMenu.SuspendLayout();
            conCntrls.SuspendLayout();
            groupBox1.SuspendLayout();
            rightPanel.SuspendLayout();
            SuspendLayout();
            // 
            // console
            // 
            console.Location = new System.Drawing.Point(13, 166);
            console.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            console.Multiline = true;
            console.Name = "console";
            console.ReadOnly = true;
            console.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            console.Size = new System.Drawing.Size(307, 149);
            console.TabIndex = 2;
            // 
            // notifyIcon
            // 
            notifyIcon.BalloonTipText = "トレイアイコンをクリックして表示します。";
            notifyIcon.BalloonTipTitle = "BetterJoy";
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Icon = (System.Drawing.Icon)resources.GetObject("notifyIcon.Icon");
            notifyIcon.Text = "BetterJoy";
            notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            // 
            // contextMenu
            // 
            contextMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { exitToolStripMenuItem });
            contextMenu.Name = "contextMenu";
            contextMenu.Size = new System.Drawing.Size(94, 26);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(93, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // version_lbl
            // 
            version_lbl.AutoSize = true;
            version_lbl.Location = new System.Drawing.Point(240, 321);
            version_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            version_lbl.Name = "version_lbl";
            version_lbl.Size = new System.Drawing.Size(28, 15);
            version_lbl.TabIndex = 2;
            version_lbl.Text = "v8.0-Gyro-JPN";
            // 
            // donationLink
            // 
            donationLink.AutoSize = true;
            donationLink.Location = new System.Drawing.Point(203, 321);
            donationLink.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            donationLink.Name = "donationLink";
            donationLink.Size = new System.Drawing.Size(43, 15);
            donationLink.TabIndex = 5;
            donationLink.TabStop = true;
            donationLink.Text = "Github";
            donationLink.LinkClicked += linkLabel1_LinkClicked;
            // 
            // conCntrls
            // 
            conCntrls.Controls.Add(loc4);
            conCntrls.Controls.Add(loc3);
            conCntrls.Controls.Add(loc2);
            conCntrls.Controls.Add(loc1);
            conCntrls.Controls.Add(con4);
            conCntrls.Controls.Add(con3);
            conCntrls.Controls.Add(con2);
            conCntrls.Controls.Add(con1);
            conCntrls.Location = new System.Drawing.Point(14, 14);
            conCntrls.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            conCntrls.Name = "conCntrls";
            conCntrls.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            conCntrls.Size = new System.Drawing.Size(304, 120);
            conCntrls.TabIndex = 0;
            conCntrls.TabStop = false;
            conCntrls.Text = "接続されたコントローラー一覧";
            // 
            // loc4
            // 
            loc4.Location = new System.Drawing.Point(231, 92);
            loc4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            loc4.Name = "loc4";
            loc4.Size = new System.Drawing.Size(68, 23);
            loc4.TabIndex = 7;
            loc4.Text = "探す";
            loc4.UseVisualStyleBackColor = true;
            // 
            // loc3
            // 
            loc3.Location = new System.Drawing.Point(156, 92);
            loc3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            loc3.Name = "loc3";
            loc3.Size = new System.Drawing.Size(68, 23);
            loc3.TabIndex = 6;
            loc3.Text = "探す";
            loc3.UseVisualStyleBackColor = true;
            // 
            // loc2
            // 
            loc2.Location = new System.Drawing.Point(82, 92);
            loc2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            loc2.Name = "loc2";
            loc2.Size = new System.Drawing.Size(68, 23);
            loc2.TabIndex = 5;
            loc2.Text = "探す";
            loc2.UseVisualStyleBackColor = true;
            // 
            // loc1
            // 
            loc1.Location = new System.Drawing.Point(7, 92);
            loc1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            loc1.Name = "loc1";
            loc1.Size = new System.Drawing.Size(68, 23);
            loc1.TabIndex = 4;
            loc1.Text = "探す";
            loc1.UseVisualStyleBackColor = true;
            // 
            // con4
            // 
            con4.BackgroundImage = Properties.Resources.cross;
            con4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            con4.Enabled = false;
            con4.Location = new System.Drawing.Point(231, 23);
            con4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            con4.Name = "con4";
            con4.Size = new System.Drawing.Size(68, 68);
            con4.TabIndex = 3;
            con4.TabStop = false;
            con4.UseVisualStyleBackColor = true;
            // 
            // con3
            // 
            con3.BackgroundImage = Properties.Resources.cross;
            con3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            con3.Enabled = false;
            con3.Location = new System.Drawing.Point(156, 23);
            con3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            con3.Name = "con3";
            con3.Size = new System.Drawing.Size(68, 68);
            con3.TabIndex = 2;
            con3.TabStop = false;
            con3.UseVisualStyleBackColor = true;
            // 
            // con2
            // 
            con2.BackgroundImage = Properties.Resources.cross;
            con2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            con2.Enabled = false;
            con2.Location = new System.Drawing.Point(82, 23);
            con2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            con2.Name = "con2";
            con2.Size = new System.Drawing.Size(68, 68);
            con2.TabIndex = 1;
            con2.TabStop = false;
            con2.UseVisualStyleBackColor = true;
            // 
            // con1
            // 
            con1.BackgroundImage = Properties.Resources.cross;
            con1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            con1.Enabled = false;
            con1.Location = new System.Drawing.Point(7, 23);
            con1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            con1.Name = "con1";
            con1.Size = new System.Drawing.Size(68, 68);
            con1.TabIndex = 0;
            con1.TabStop = false;
            btnTip.SetToolTip(con1, "Click on Joycons to join/split them");
            con1.UseVisualStyleBackColor = true;
            // 
            // foldLbl
            // 
            foldLbl.Location = new System.Drawing.Point(320, 23);
            foldLbl.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            foldLbl.Name = "foldLbl";
            foldLbl.Size = new System.Drawing.Size(15, 267);
            foldLbl.TabIndex = 12;
            foldLbl.Text = ">";
            foldLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            btnTip.SetToolTip(foldLbl, "設定");
            foldLbl.Click += foldLbl_Click;
            // 
            // startInTrayBox
            // 
            startInTrayBox.AutoSize = true;
            startInTrayBox.Location = new System.Drawing.Point(13, 321);
            startInTrayBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            startInTrayBox.Name = "startInTrayBox";
            startInTrayBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            startInTrayBox.Size = new System.Drawing.Size(87, 19);
            startInTrayBox.TabIndex = 6;
            startInTrayBox.Text = "トレイで開始";
            startInTrayBox.UseVisualStyleBackColor = true;
            startInTrayBox.CheckedChanged += startInTrayBox_CheckedChanged;
            // 
            // btn_open3rdP
            // 
            btn_open3rdP.Location = new System.Drawing.Point(110, 135);
            btn_open3rdP.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btn_open3rdP.Name = "btn_open3rdP";
            btn_open3rdP.Size = new System.Drawing.Size(120, 25);
            btn_open3rdP.TabIndex = 7;
            btn_open3rdP.Text = "コントローラーを追加";
            btn_open3rdP.UseVisualStyleBackColor = true;
            btn_open3rdP.Click += btn_open3rdP_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(settingsTable);
            groupBox1.Location = new System.Drawing.Point(4, 13);
            groupBox1.Margin = new System.Windows.Forms.Padding(2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(2);
            groupBox1.Size = new System.Drawing.Size(355, 298);
            groupBox1.TabIndex = 9;
            groupBox1.TabStop = false;
            groupBox1.Text = "Config";
            // 
            // settingsTable
            // 
            settingsTable.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            settingsTable.AutoScroll = true;
            settingsTable.ColumnCount = 2;
            settingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58.90411F));
            settingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 41.09589F));
            settingsTable.Location = new System.Drawing.Point(5, 20);
            settingsTable.Margin = new System.Windows.Forms.Padding(2);
            settingsTable.Name = "settingsTable";
            settingsTable.RowCount = 1;
            settingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            settingsTable.Size = new System.Drawing.Size(350, 274);
            settingsTable.TabIndex = 1;
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(settingsApply);
            rightPanel.Controls.Add(groupBox1);
            rightPanel.Location = new System.Drawing.Point(337, 1);
            rightPanel.Margin = new System.Windows.Forms.Padding(2, 2, 14, 2);
            rightPanel.Name = "rightPanel";
            rightPanel.Size = new System.Drawing.Size(364, 343);
            rightPanel.TabIndex = 11;
            rightPanel.Visible = false;
            // 
            // settingsApply
            // 
            settingsApply.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            settingsApply.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            settingsApply.Location = new System.Drawing.Point(288, 315);
            settingsApply.Margin = new System.Windows.Forms.Padding(2);
            settingsApply.Name = "settingsApply";
            settingsApply.Size = new System.Drawing.Size(71, 24);
            settingsApply.TabIndex = 10;
            settingsApply.Text = "Apply";
            settingsApply.UseVisualStyleBackColor = true;
            settingsApply.Click += settingsApply_Click;
            // 
            // AutoCalibrate
            // 
            AutoCalibrate.Location = new System.Drawing.Point(235, 135);
            AutoCalibrate.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            AutoCalibrate.Name = "AutoCalibrate";
            AutoCalibrate.Size = new System.Drawing.Size(83, 25);
            AutoCalibrate.TabIndex = 8;
            AutoCalibrate.Text = "Calibrate";
            AutoCalibrate.UseVisualStyleBackColor = true;
            AutoCalibrate.Click += StartCalibrate;
            // 
            // btn_reassign_open
            // 
            btn_reassign_open.Location = new System.Drawing.Point(14, 135);
            btn_reassign_open.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btn_reassign_open.Name = "btn_reassign_open";
            btn_reassign_open.Size = new System.Drawing.Size(88, 25);
            btn_reassign_open.TabIndex = 13;
            btn_reassign_open.Text = "ボタンマッピング";
            btn_reassign_open.UseVisualStyleBackColor = true;
            btn_reassign_open.Click += btn_reassign_open_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(718, 347);
            Controls.Add(btn_reassign_open);
            Controls.Add(foldLbl);
            Controls.Add(rightPanel);
            Controls.Add(AutoCalibrate);
            Controls.Add(btn_open3rdP);
            Controls.Add(startInTrayBox);
            Controls.Add(conCntrls);
            Controls.Add(donationLink);
            Controls.Add(version_lbl);
            Controls.Add(console);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimumSize = new System.Drawing.Size(0, 376);
            Name = "MainForm";
            Text = "BetterJoy v8.0 Gyro-JPN-Addon";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Resize += MainForm_Resize;
            contextMenu.ResumeLayout(false);
            conCntrls.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public System.Windows.Forms.TextBox console;
        public System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.Label version_lbl;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.LinkLabel donationLink;
        private System.Windows.Forms.GroupBox conCntrls;
        private System.Windows.Forms.Button con1;
        private System.Windows.Forms.Button con4;
        private System.Windows.Forms.Button con3;
        private System.Windows.Forms.Button con2;
        private System.Windows.Forms.Button loc4;
        private System.Windows.Forms.Button loc3;
        private System.Windows.Forms.Button loc2;
        private System.Windows.Forms.Button loc1;
        private System.Windows.Forms.ToolTip btnTip;
        private System.Windows.Forms.CheckBox startInTrayBox;
        private System.Windows.Forms.Button btn_open3rdP;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel settingsTable;
        private System.Windows.Forms.Panel rightPanel;
        private System.Windows.Forms.Button settingsApply;
        private System.Windows.Forms.Label foldLbl;
        private System.Windows.Forms.Button AutoCalibrate;
        private System.Windows.Forms.Button btn_reassign_open;
    }
}
