namespace WavMicBridge
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox txtFile;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.ComboBox cmbDevice;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.CheckBox chkLoop;
        private System.Windows.Forms.TrackBar volumeTrack;
        private System.Windows.Forms.Label lblVol;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;

        // 麦克风测试 UI
        private System.Windows.Forms.ComboBox cmbMic;
        private System.Windows.Forms.Button btnMicTest;
        private System.Windows.Forms.ProgressBar prgMicLevel;
        private System.Windows.Forms.Label lblMicLevelText;
        private System.Windows.Forms.GroupBox grpMic;

        // 声音库 UI
        private System.Windows.Forms.GroupBox grpSoundBank;
        private System.Windows.Forms.ListView lvSoundBank;
        private System.Windows.Forms.ColumnHeader colSlot;
        private System.Windows.Forms.ColumnHeader colFile;
        private System.Windows.Forms.ContextMenuStrip cmsSound;
        private System.Windows.Forms.ToolStripMenuItem miBind;
        private System.Windows.Forms.ToolStripMenuItem miPlay;
        private System.Windows.Forms.ToolStripMenuItem miClear;
        private System.Windows.Forms.ToolStripMenuItem miOpenFolder;
        private System.Windows.Forms.Label lblHotkeyHint;

        // ==== 托盘相关 ====
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.ToolStripMenuItem trayMiShowHide;
        private System.Windows.Forms.ToolStripMenuItem trayMiAbout;
        private System.Windows.Forms.ToolStripMenuItem trayMiExit;
        private System.Windows.Forms.ToolStripSeparator traySep;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.txtFile = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.cmbDevice = new System.Windows.Forms.ComboBox();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.chkLoop = new System.Windows.Forms.CheckBox();
            this.volumeTrack = new System.Windows.Forms.TrackBar();
            this.lblVol = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label(); // 输出设备
            this.label2 = new System.Windows.Forms.Label(); // 文件

            this.grpMic = new System.Windows.Forms.GroupBox();
            this.cmbMic = new System.Windows.Forms.ComboBox();
            this.btnMicTest = new System.Windows.Forms.Button();
            this.prgMicLevel = new System.Windows.Forms.ProgressBar();
            this.lblMicLevelText = new System.Windows.Forms.Label();

            this.grpSoundBank = new System.Windows.Forms.GroupBox();
            this.lvSoundBank = new System.Windows.Forms.ListView();
            this.colSlot = new System.Windows.Forms.ColumnHeader();
            this.colFile = new System.Windows.Forms.ColumnHeader();
            this.cmsSound = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miBind = new System.Windows.Forms.ToolStripMenuItem();
            this.miPlay = new System.Windows.Forms.ToolStripMenuItem();
            this.miClear = new System.Windows.Forms.ToolStripMenuItem();
            this.miOpenFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.lblHotkeyHint = new System.Windows.Forms.Label();

            // ==== 托盘相关 ====
            this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.trayMiShowHide = new System.Windows.Forms.ToolStripMenuItem();
            this.traySep = new System.Windows.Forms.ToolStripSeparator();
            this.trayMiAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.trayMiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);

            ((System.ComponentModel.ISupportInitialize)(this.volumeTrack)).BeginInit();
            this.grpMic.SuspendLayout();
            this.grpSoundBank.SuspendLayout();
            this.cmsSound.SuspendLayout();

            // 窗体
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(820, 560);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WAV → 虚拟麦克风 桥";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);

            // txtFile
            this.txtFile.Location = new System.Drawing.Point(80, 20);
            this.txtFile.Size = new System.Drawing.Size(610, 23);

            // btnBrowse
            this.btnBrowse.Location = new System.Drawing.Point(700, 19);
            this.btnBrowse.Size = new System.Drawing.Size(90, 25);
            this.btnBrowse.Text = "浏览...";
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);

            // label2 “文件”
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 24);
            this.label2.Text = "文件";

            // label1 “输出设备”
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 58);
            this.label1.Text = "输出设备";

            // cmbDevice
            this.cmbDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDevice.Location = new System.Drawing.Point(80, 55);
            this.cmbDevice.Size = new System.Drawing.Size(400, 23);

            // btnPlay
            this.btnPlay.Location = new System.Drawing.Point(500, 54);
            this.btnPlay.Size = new System.Drawing.Size(90, 25);
            this.btnPlay.Text = "播放";
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);

            // btnStop
            this.btnStop.Location = new System.Drawing.Point(600, 54);
            this.btnStop.Size = new System.Drawing.Size(90, 25);
            this.btnStop.Text = "停止";
            this.btnStop.Enabled = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);

            // chkLoop
            this.chkLoop.AutoSize = true;
            this.chkLoop.Location = new System.Drawing.Point(700, 58);
            this.chkLoop.Text = "循环";

            // volumeTrack
            this.volumeTrack.Location = new System.Drawing.Point(80, 90);
            this.volumeTrack.Size = new System.Drawing.Size(540, 45);
            this.volumeTrack.Minimum = 0;
            this.volumeTrack.Maximum = 100;
            this.volumeTrack.TickFrequency = 5;
            this.volumeTrack.Value = 80;
            this.volumeTrack.Scroll += new System.EventHandler(this.volumeTrack_Scroll);

            // lblVol
            this.lblVol.AutoSize = true;
            this.lblVol.Location = new System.Drawing.Point(640, 100);
            this.lblVol.Text = "80%";

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(20, 130);
            this.lblStatus.Size = new System.Drawing.Size(760, 15);
            this.lblStatus.Text = "就绪";

            // ===== 麦克风测试分组 =====
            this.grpMic.Location = new System.Drawing.Point(20, 160);
            this.grpMic.Size = new System.Drawing.Size(770, 110);
            this.grpMic.Text = "麦克风测试";

            // cmbMic
            this.cmbMic.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMic.Location = new System.Drawing.Point(20, 25);
            this.cmbMic.Size = new System.Drawing.Size(550, 23);

            // btnMicTest
            this.btnMicTest.Location = new System.Drawing.Point(590, 24);
            this.btnMicTest.Size = new System.Drawing.Size(150, 25);
            this.btnMicTest.Text = "测试麦克风";
            this.btnMicTest.Click += new System.EventHandler(this.btnMicTest_Click);

            // prgMicLevel
            this.prgMicLevel.Location = new System.Drawing.Point(20, 65);
            this.prgMicLevel.Size = new System.Drawing.Size(650, 18);
            this.prgMicLevel.Maximum = 100;

            // lblMicLevelText
            this.lblMicLevelText.AutoSize = true;
            this.lblMicLevelText.Location = new System.Drawing.Point(680, 66);
            this.lblMicLevelText.Text = "0 / 100";

            this.grpMic.Controls.Add(this.cmbMic);
            this.grpMic.Controls.Add(this.btnMicTest);
            this.grpMic.Controls.Add(this.prgMicLevel);
            this.grpMic.Controls.Add(this.lblMicLevelText);

            // ===== 声音库分组 =====
            this.grpSoundBank.Location = new System.Drawing.Point(20, 280);
            this.grpSoundBank.Size = new System.Drawing.Size(770, 250);
            this.grpSoundBank.Text = "声音库（Ctrl + Alt + W + 1..9）";

            // lvSoundBank
            this.lvSoundBank.Location = new System.Drawing.Point(20, 25);
            this.lvSoundBank.Size = new System.Drawing.Size(730, 180);
            this.lvSoundBank.View = System.Windows.Forms.View.Details;
            this.lvSoundBank.FullRowSelect = true;
            this.lvSoundBank.GridLines = true;
            this.lvSoundBank.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvSoundBank.HideSelection = false;
            this.lvSoundBank.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colSlot, this.colFile
            });
            this.lvSoundBank.ContextMenuStrip = this.cmsSound;
            this.lvSoundBank.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvSoundBank_MouseDoubleClick);

            // 列
            this.colSlot.Text = "槽位";
            this.colSlot.Width = 80;
            this.colFile.Text = "已绑定文件";
            this.colFile.Width = 620;

            // 右键菜单（声音库）
            this.cmsSound.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.miBind, this.miPlay, this.miClear, this.miOpenFolder
            });

            this.miBind.Text = "绑定/重新绑定...";
            this.miBind.Click += new System.EventHandler(this.miBind_Click);

            this.miPlay.Text = "播放（与热键一致）";
            this.miPlay.Click += new System.EventHandler(this.miPlay_Click);

            this.miClear.Text = "清除绑定";
            this.miClear.Click += new System.EventHandler(this.miClear_Click);

            this.miOpenFolder.Text = "打开文件位置";
            this.miOpenFolder.Click += new System.EventHandler(this.miOpenFolder_Click);

            // 热键提示
            this.lblHotkeyHint.AutoSize = true;
            this.lblHotkeyHint.Location = new System.Drawing.Point(20, 215);
            this.lblHotkeyHint.Text = "提示：首按槽位热键会弹窗绑定 WAV；按住 Shift 使用热键可强制重绑。双击列表项 = 播放。";

            this.grpSoundBank.Controls.Add(this.lvSoundBank);
            this.grpSoundBank.Controls.Add(this.lblHotkeyHint);

            // ==== 托盘：菜单与图标 ====
            this.trayMiShowHide.Text = "打开主窗口";
            this.trayMiAbout.Text = "关于";
            this.trayMiExit.Text = "退出";
            this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.trayMiShowHide, this.traySep, this.trayMiAbout, this.trayMiExit
            });

            // 托盘图标（图标图像在 MainForm.cs 设置）
            this.trayIcon.ContextMenuStrip = this.trayMenu;
            this.trayIcon.Text = "WavMicBridge";
            this.trayIcon.Visible = false;

            // 添加到窗体
            this.Controls.Add(this.grpSoundBank);
            this.Controls.Add(this.grpMic);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblVol);
            this.Controls.Add(this.volumeTrack);
            this.Controls.Add(this.chkLoop);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.cmbDevice);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtFile);
            this.Controls.Add(this.label2);

            ((System.ComponentModel.ISupportInitialize)(this.volumeTrack)).EndInit();
            this.grpMic.ResumeLayout(false);
            this.grpMic.PerformLayout();
            this.grpSoundBank.ResumeLayout(false);
            this.grpSoundBank.PerformLayout();
            this.cmsSound.ResumeLayout(false);
        }
    }
}
