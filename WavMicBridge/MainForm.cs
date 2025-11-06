using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Timer = System.Windows.Forms.Timer;
using CMLS.CLogger; // 日志

namespace WavMicBridge
{
    public partial class MainForm : Form
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("MainForm");

        private MMDevice? _renderDevice;
        private WasapiOut? _player;
        private ISampleProvider? _pipeline;
        private AudioFileReader? _reader;
        private LoopStream? _looper;

        private MMDevice? _captureDevice;
        private WasapiCapture? _micCapture;
        private readonly Timer _meterTimer = new Timer { Interval = 50 };
        private float _lastPeak = 0f;

        // 声音库
        private const int SLOT_MIN = 1;
        private const int SLOT_MAX = 9;
        private readonly Dictionary<int, string> _soundBank = new();
        private readonly string _soundBankPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "soundbank.json");

        // 全局热键：低层键盘钩子
        private static IntPtr _kbHook = IntPtr.Zero;
        private static LowLevelKeyboardProc? _kbProc;
        private static readonly HashSet<Keys> _pressed = new();
        private DateTime _lastTriggerAt = DateTime.MinValue;
        private int _lastTriggerSlot = -1;

        // ==== 托盘相关 ====
        private NotifyIcon _trayIcon = null!;
        private ContextMenuStrip _trayMenu = null!;
        private ToolStripMenuItem _miShowHide = null!;
        private ToolStripMenuItem _miAbout = null!;
        private ToolStripMenuItem _miExit = null!;
        private bool _reallyExit = false;
        private bool _trayTipShown = false;

        public MainForm()
        {
            InitializeComponent();

            LOGGER.Info("MainForm 初始化开始。");

            try
            {
                LoadRenderDevices();
                LoadCaptureDevices();
                LOGGER.Info("音频设备枚举完成。");

                volumeTrack.Value = 80;
                lblVol.Text = "80%";
                lblStatus.Text = "就绪";

                _meterTimer.Tick += MeterTimer_Tick;

                if (!IsCableInstalled())
                {
                    lblStatus.Text = "未检测到虚拟麦驱动：当前将播放到默认输出设备";
                    LOGGER.Warn("未检测到虚拟麦驱动（Virtual Audio Cable / VoiceMeeter）。");
                }
                else
                {
                    lblStatus.Text = "检测到虚拟麦克风：请在游戏中选择麦克风『CABLE Output』";
                    LOGGER.Info("检测到虚拟音频设备：CABLE Input/Output 可用。");
                }

                LoadSoundBank();
                RefreshSoundList();
                InstallKeyboardHook();

                // 初始化托盘
                BuildTray();

                LOGGER.Info("MainForm 初始化完成。");
            }
            catch (Exception ex)
            {
                LOGGER.Error("MainForm 初始化异常：" + ex);
            }
        }

        private void BuildTray()
        {
            LOGGER.Debug("构建托盘菜单与图标。");
            _trayMenu = new ContextMenuStrip();
            _miShowHide = new ToolStripMenuItem("打开主窗口", null, (_, __) => ToggleMainWindow());
            _miAbout = new ToolStripMenuItem("关于", null, (_, __) => ShowAbout());
            _miExit = new ToolStripMenuItem("退出", null, (_, __) => ExitApp());
            _trayMenu.Items.AddRange(new ToolStripItem[]
            {
                _miShowHide,
                new ToolStripSeparator(),
                _miAbout,
                _miExit
            });

            _trayIcon = new NotifyIcon
            {
                Text = "WavMicBridge",
                Icon = SystemIcons.Application,   // 可替换为自定义 .ico
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            _trayIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left) ToggleMainWindow();
            };
            UpdateTrayMenuText();
        }

        private void ToggleMainWindow()
        {
            bool willHide = this.Visible && this.WindowState != FormWindowState.Minimized;
            LOGGER.Info(willHide ? "用户操作：隐藏主窗口（托盘驻留）。" : "用户操作：从托盘恢复主窗口。");

            if (willHide)
            {
                HideToTray();
            }
            else
            {
                ShowFromTray();
            }
            UpdateTrayMenuText();
        }

        private void HideToTray()
        {
            if (!_trayTipShown)
            {
                try { _trayIcon.ShowBalloonTip(1500, "已最小化到托盘", "左键点击图标可再次打开窗口。", ToolTipIcon.Info); } catch { }
                _trayTipShown = true;
            }
            this.ShowInTaskbar = false;
            this.Hide();
            LOGGER.Debug("窗口已隐藏至托盘。");
        }

        private void ShowFromTray()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            this.BringToFront();
            LOGGER.Debug("窗口已从托盘恢复。");
        }

        private void UpdateTrayMenuText()
        {
            if (_miShowHide == null) return;
            _miShowHide.Text = this.Visible ? "隐藏主窗口" : "打开主窗口";
        }

        private void ShowAbout()
        {
            string ver = Application.ProductVersion;
            LOGGER.Info("显示『关于』对话框。");
            MessageBox.Show(
                $"WavMicBridge\n版本：{ver}\n\n" +
                "作者：@Convex89524\n" +
                "说明：支持托盘常驻后台，热键快速播放声音库，麦克风电平可视化测试。\n" +
                "建议：将输出设备选为 CABLE Input，并在游戏中选择麦克风『CABLE Output』。",
                "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitApp()
        {
            LOGGER.Warn("收到退出请求。");
            _reallyExit = true;
            try { _trayIcon.Visible = false; } catch { }
            this.Close();
        }

        private void RefreshSoundList()
        {
            LOGGER.Debug("刷新声音库列表视图。");
            lvSoundBank.BeginUpdate();
            lvSoundBank.Items.Clear();
            for (int i = SLOT_MIN; i <= SLOT_MAX; i++)
            {
                string file = _soundBank.TryGetValue(i, out var p) && File.Exists(p) ? p : "（未绑定）";
                var it = new ListViewItem(new[] { $"槽位 {i}", file }) { Tag = i };
                lvSoundBank.Items.Add(it);
            }
            lvSoundBank.EndUpdate();
        }

        private int? GetSelectedSlot()
        {
            if (lvSoundBank.SelectedItems.Count == 0) return null;
            return (int)lvSoundBank.SelectedItems[0].Tag!;
        }

        private void lvSoundBank_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            var slot = GetSelectedSlot();
            if (slot.HasValue)
            {
                LOGGER.Info($"双击播放槽位：{slot.Value}");
                TriggerSlot(slot.Value, forceRebind: false, fromNumpad: false);
            }
        }

        private void miBind_Click(object? sender, EventArgs e)
        {
            var slot = GetSelectedSlot();
            if (!slot.HasValue) return;
            LOGGER.Info($"请求绑定槽位：{slot.Value}");
            TriggerSlot(slot.Value, forceRebind: true, fromNumpad: false);
            RefreshSoundList();
        }

        private void miPlay_Click(object? sender, EventArgs e)
        {
            var slot = GetSelectedSlot();
            if (!slot.HasValue) return;
            LOGGER.Info($"请求播放槽位：{slot.Value}");
            TriggerSlot(slot.Value, forceRebind: false, fromNumpad: false);
        }

        private void miClear_Click(object? sender, EventArgs e)
        {
            var slot = GetSelectedSlot();
            if (!slot.HasValue) return;
            if (_soundBank.ContainsKey(slot.Value))
            {
                LOGGER.Info($"清除槽位绑定：{slot.Value}");
                _soundBank.Remove(slot.Value);
                SaveSoundBank();
                RefreshSoundList();
            }
        }

        private void miOpenFolder_Click(object? sender, EventArgs e)
        {
            var slot = GetSelectedSlot();
            if (!slot.HasValue) return;
            if (_soundBank.TryGetValue(slot.Value, out var path) && File.Exists(path))
            {
                LOGGER.Debug($"打开文件位置：{path}");
                try { Process.Start("explorer.exe", $"/select,\"{path}\""); } catch (Exception ex) { LOGGER.Warn("打开文件位置失败：" + ex.Message); }
            }
        }

        private void LoadSoundBank()
        {
            LOGGER.Info("加载声音库...");
            try
            {
                if (File.Exists(_soundBankPath))
                {
                    var json = File.ReadAllText(_soundBankPath);
                    var map = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
                    _soundBank.Clear();
                    if (map != null)
                    {
                        foreach (var kv in map)
                        {
                            if (kv.Key >= SLOT_MIN && kv.Key <= SLOT_MAX && !string.IsNullOrWhiteSpace(kv.Value))
                                _soundBank[kv.Key] = kv.Value;
                        }
                    }
                }
                LOGGER.Info($"声音库加载完成，已绑定 { _soundBank.Count } 个槽位。");
            }
            catch (Exception ex)
            {
                LOGGER.Error("加载声音库时发生错误：" + ex);
            }
        }

        private void SaveSoundBank()
        {
            try
            {
                var json = JsonSerializer.Serialize(_soundBank, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_soundBankPath, json);
                LOGGER.Info("声音库保存完成。");
            }
            catch (Exception ex)
            {
                LOGGER.Error("保存声音库时发生错误：" + ex);
            }
        }

        private void TriggerSlot(int slot, bool forceRebind, bool fromNumpad)
        {
            if (slot < SLOT_MIN || slot > SLOT_MAX) return;

            LOGGER.Info($"触发槽位：{slot}（强制重绑={forceRebind}，来自小键盘={fromNumpad}）");

            if (forceRebind || !_soundBank.TryGetValue(slot, out var path) || !File.Exists(path))
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "WAV 文件 (*.wav)|*.wav|所有文件 (*.*)|*.*",
                    Title = $"选择要绑定到槽位 {slot} 的 WAV 文件"
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _soundBank[slot] = ofd.FileName;
                    SaveSoundBank();
                    RefreshSoundList();
                    PlayFromPath(ofd.FileName);
                    lblStatus.Text = $"已通过槽位 {slot} 播放：{Path.GetFileName(ofd.FileName)}";
                    LOGGER.Info($"槽位 {slot} 绑定并播放：{ofd.FileName}");
                }
                return;
            }

            PlayFromPath(path);
            lblStatus.Text = $"已通过槽位 {slot} 播放：{Path.GetFileName(path)}";
            LOGGER.Info($"槽位 {slot} 播放：{path}");
        }

        private void PlayFromPath(string path)
        {
            LOGGER.Debug($"尝试播放文件：{path}");

            if (!File.Exists(path))
            {
                LOGGER.Warn($"文件不存在：{path}");
                MessageBox.Show("文件不存在，是否需要重新绑定该槽位？", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (cmbDevice.SelectedItem is not DeviceItem item)
            {
                LOGGER.Warn("未选择输出设备。");
                MessageBox.Show("请选择一个输出设备（推荐 CABLE Input）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                StopPlayback();

                _renderDevice = item.Device;

                _reader = new AudioFileReader(path) { Volume = volumeTrack.Value / 100f };

                ISampleProvider sample = _reader.ToSampleProvider();
                if (_reader.WaveFormat.Channels == 2)
                {
                    sample = new StereoToMonoSampleProvider(sample)
                    {
                        LeftVolume = 0.5f,
                        RightVolume = 0.5f
                    };
                }

                sample = new WdlResamplingSampleProvider(sample, 48000);

                if (chkLoop.Checked)
                {
                    _looper = new LoopStream(sample.ToWaveProvider());
                    _pipeline = _looper.ToSampleProvider();
                }
                else
                {
                    _looper = null;
                    _pipeline = sample;
                }

                _player = new WasapiOut(_renderDevice, AudioClientShareMode.Shared, true, 100);
                _player.Init(_pipeline.ToWaveProvider());
                _player.Play();

                btnPlay.Enabled = false;
                btnStop.Enabled = true;

                if (_renderDevice.FriendlyName.IndexOf("CABLE Input", StringComparison.OrdinalIgnoreCase) >= 0)
                    lblStatus.Text = $"播放中 → {_renderDevice.FriendlyName} | 请在游戏中选择麦克风『CABLE Output』";
                else
                    lblStatus.Text = $"播放中 → {_renderDevice.FriendlyName}";

                LOGGER.Info($"开始播放：{Path.GetFileName(path)} → 输出设备：{_renderDevice.FriendlyName}（循环={chkLoop.Checked}，音量={volumeTrack.Value}%）");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "错误";
                LOGGER.Error($"播放失败：{path}\n{ex}");
                MessageBox.Show("播放失败：\r\n" + ex, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopPlayback();
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "WAV 文件 (*.wav)|*.wav|所有文件 (*.*)|*.*",
                Title = "选择要播放的 WAV 文件"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = ofd.FileName;
                LOGGER.Info($"选择文件：{ofd.FileName}");
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtFile.Text))
            {
                LOGGER.Warn("『播放』被拒：未选择有效 WAV 文件。");
                MessageBox.Show("请选择有效的 WAV 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PlayFromPath(txtFile.Text);
        }

        private void btnStop_Click(object sender, EventArgs e) => StopPlayback();

        private void StopPlayback()
        {
            LOGGER.Debug("停止播放流程开始。");
            try { _player?.Stop(); } catch (Exception ex) { LOGGER.Debug("停止播放器时异常：" + ex.Message); }
            finally { _player?.Dispose(); _player = null; }

            _reader?.Dispose(); _reader = null;
            _looper = null;
            _pipeline = null;

            btnPlay.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Text = "已停止";
            LOGGER.Info("播放已停止。");
        }

        private void volumeTrack_Scroll(object sender, EventArgs e)
        {
            if (_reader != null) _reader.Volume = volumeTrack.Value / 100f;
            lblVol.Text = $"{volumeTrack.Value}%";
            LOGGER.Debug($"音量滑块：{volumeTrack.Value}%");
        }

        private void cmbMic_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LOGGER.Debug("麦克风选择变更。");
            if (_micCapture != null)
            {
                StopMicTest();
                StartMicTest();
            }
        }

        private void btnMicTest_Click(object? sender, EventArgs e)
        {
            if (_micCapture == null) StartMicTest();
            else StopMicTest();
        }

        private void StartMicTest()
        {
            if (cmbMic.SelectedItem is not DeviceItem micItem)
            {
                LOGGER.Warn("开始麦克风测试失败：未选择输入设备。");
                MessageBox.Show("请选择一个输入设备（麦克风）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                StopMicTest();

                _captureDevice = micItem.Device;
                _micCapture = new WasapiCapture(_captureDevice)
                {
                    ShareMode = AudioClientShareMode.Shared
                };
                _micCapture.DataAvailable += MicCapture_DataAvailable;
                _micCapture.RecordingStopped += MicCapture_RecordingStopped;
                _micCapture.StartRecording();

                _lastPeak = 0f;
                prgMicLevel.Value = 0;
                lblMicLevelText.Text = "0 / 100";

                _meterTimer.Start();
                btnMicTest.Text = "停止测试";
                lblStatus.Text = $"麦克风测试中 → {_captureDevice.FriendlyName}";

                LOGGER.Info($"开始麦克风测试：{_captureDevice.FriendlyName}");
            }
            catch (Exception ex)
            {
                LOGGER.Error("无法开始麦克风测试：" + ex);
                MessageBox.Show("无法开始麦克风测试：\r\n" + ex, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopMicTest();
            }
        }

        private void StopMicTest()
        {
            _meterTimer.Stop();

            if (_micCapture != null)
            {
                try { _micCapture.StopRecording(); } catch (Exception ex) { LOGGER.Debug("停止录音异常：" + ex.Message); }
                _micCapture.DataAvailable -= MicCapture_DataAvailable;
                _micCapture.RecordingStopped -= MicCapture_RecordingStopped;
                _micCapture.Dispose();
                _micCapture = null;
            }

            _captureDevice = null;
            btnMicTest.Text = "测试麦克风";
            lblStatus.Text = "就绪";

            LOGGER.Info("麦克风测试已停止。");
        }

        private void MicCapture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            // 这里不处理数据，仅通过 MMDevice 的 AudioMeterInformation 取电平
            // 可按需添加 TRACE 级别日志（高频，不建议）
        }

        private void MicCapture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            LOGGER.Debug("麦克风录音已停止（RecordingStopped 事件）。");
            if (InvokeRequired)
            {
                BeginInvoke(new Action(StopMicTest));
            }
            else
            {
                StopMicTest();
            }
        }

        private void MeterTimer_Tick(object? sender, EventArgs e)
        {
            if (_captureDevice == null) return;

            float peak = 0f;
            try { peak = _captureDevice.AudioMeterInformation.MasterPeakValue; } catch { }

            const float smooth = 0.5f;
            _lastPeak = _lastPeak * smooth + peak * (1f - smooth);

            int value = (int)Math.Round(Math.Max(0, Math.Min(1.0f, _lastPeak)) * 100.0f);
            value = Math.Max(0, Math.Min(100, value));

            if (prgMicLevel.Value != value) prgMicLevel.Value = value;
            lblMicLevelText.Text = $"{value} / 100";
        }

        private void LoadRenderDevices()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            cmbDevice.Items.Clear();
            foreach (var d in devices) cmbDevice.Items.Add(new DeviceItem(d));

            int idx = FindPreferredRenderIndex(devices);
            if (idx >= 0) cmbDevice.SelectedIndex = idx;

            if (devices.Count == 0)
            {
                lblStatus.Text = "未发现任何输出设备";
                LOGGER.Warn("未发现任何输出设备。");
            }
            else
            {
                LOGGER.Info($"发现输出设备 {devices.Count} 个，已选择索引 {idx}。");
            }
        }

        private void LoadCaptureDevices()
        {
            var enumerator = new MMDeviceEnumerator();
            var caps = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
            cmbMic.Items.Clear();
            foreach (var d in caps) cmbMic.Items.Add(new DeviceItem(d));

            int idx = -1;
            string[] prefer = { "CABLE Output", "VoiceMeeter Output", "Virtual Audio Cable" };
            foreach (var key in prefer)
            {
                idx = caps.FindIndex(d => d.FriendlyName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
                if (idx >= 0) break;
            }
            if (idx < 0 && caps.Count > 0) idx = 0;
            if (idx >= 0) cmbMic.SelectedIndex = idx;

            LOGGER.Info($"发现输入设备 {caps.Count} 个，已选择索引 {idx}。");
        }

        private static int FindPreferredRenderIndex(List<MMDevice> renderList)
        {
            if (renderList.Count == 0) return -1;
            string[] prefer = { "CABLE Input", "VoiceMeeter Input", "Virtual Audio Cable" };
            for (int p = 0; p < prefer.Length; p++)
            {
                int i = renderList.FindIndex(d =>
                    d.FriendlyName.IndexOf(prefer[p], StringComparison.OrdinalIgnoreCase) >= 0);
                if (i >= 0) return i;
            }
            return renderList.Count > 0 ? 0 : -1;
        }

        private static bool IsCableInstalled()
        {
            var enu = new MMDeviceEnumerator();
            bool recOk = enu.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                            .Any(d => d.FriendlyName.IndexOf("CABLE Output", StringComparison.OrdinalIgnoreCase) >= 0);
            bool renderOk = enu.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                               .Any(d => d.FriendlyName.IndexOf("CABLE Input", StringComparison.OrdinalIgnoreCase) >= 0);
            return recOk && renderOk;
        }

        private void InstallKeyboardHook()
        {
            if (_kbHook != IntPtr.Zero) return;
            _kbProc = KeyboardHookProc;
            _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, GetModuleHandle(null), 0);
            LOGGER.Info("全局键盘钩子已安装。");
        }

        private void UninstallKeyboardHook()
        {
            if (_kbHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_kbHook);
                _kbHook = IntPtr.Zero;
                LOGGER.Info("全局键盘钩子已卸载。");
            }
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (KeyboardMessage)wParam;
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var key = (Keys)data.vkCode;

                if (msg == KeyboardMessage.WM_KEYDOWN || msg == KeyboardMessage.WM_SYSKEYDOWN)
                {
                    _pressed.Add(key);
                    TryHandleChord(key, isKeyDown: true);
                }
                else if (msg == KeyboardMessage.WM_KEYUP || msg == KeyboardMessage.WM_SYSKEYUP)
                {
                    TryHandleChord(key, isKeyDown: false);
                    _pressed.Remove(key);
                }
            }
            return CallNextHookEx(_kbHook, nCode, wParam, lParam);
        }

        private void TryHandleChord(Keys key, bool isKeyDown)
        {
            if (isKeyDown) return;

            bool ctrl = _pressed.Contains(Keys.LControlKey) || _pressed.Contains(Keys.RControlKey) || _pressed.Contains(Keys.ControlKey);
            bool alt  = _pressed.Contains(Keys.LMenu) || _pressed.Contains(Keys.RMenu) || _pressed.Contains(Keys.Menu);
            bool wKey = _pressed.Contains(Keys.W);

            if (!(ctrl && alt && wKey)) return;

            bool shift = _pressed.Contains(Keys.LShiftKey) || _pressed.Contains(Keys.RShiftKey) || _pressed.Contains(Keys.ShiftKey);
            int slot = KeyToSlot(key, out bool fromNumpad);
            if (slot == -1) return;

            var now = DateTime.UtcNow;
            if (slot == _lastTriggerSlot && (now - _lastTriggerAt).TotalMilliseconds < 250) return;
            _lastTriggerSlot = slot;
            _lastTriggerAt = now;

            LOGGER.Info($"热键触发：Ctrl+Alt+W+{slot}（Shift={shift}，Pad={fromNumpad}）");

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    TriggerSlot(slot, forceRebind: shift, fromNumpad: fromNumpad);
                }));
            }
        }

        private static int KeyToSlot(Keys key, out bool fromNumpad)
        {
            fromNumpad = false;
            switch (key)
            {
                case Keys.D1: return 1;
                case Keys.D2: return 2;
                case Keys.D3: return 3;
                case Keys.D4: return 4;
                case Keys.D5: return 5;
                case Keys.D6: return 6;
                case Keys.D7: return 7;
                case Keys.D8: return 8;
                case Keys.D9: return 9;
                case Keys.NumPad1: fromNumpad = true; return 1;
                case Keys.NumPad2: fromNumpad = true; return 2;
                case Keys.NumPad3: fromNumpad = true; return 3;
                case Keys.NumPad4: fromNumpad = true; return 4;
                case Keys.NumPad5: fromNumpad = true; return 5;
                case Keys.NumPad6: fromNumpad = true; return 6;
                case Keys.NumPad7: fromNumpad = true; return 7;
                case Keys.NumPad8: fromNumpad = true; return 8;
                case Keys.NumPad9: fromNumpad = true; return 9;
                default: return -1;
            }
        }

        // P/Invoke
        private const int WH_KEYBOARD_LL = 13;
        private enum KeyboardMessage { WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105 }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private sealed class DeviceItem
        {
            public MMDevice Device { get; }
            public DeviceItem(MMDevice d) => Device = d;
            public override string ToString() => Device.FriendlyName;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_reallyExit)
            {
                LOGGER.Info("关闭事件被转换为隐藏到托盘。");
                e.Cancel = true;
                HideToTray();
                UpdateTrayMenuText();
                return;
            }

            LOGGER.Warn("应用即将退出，清理资源。");
            try { _trayIcon.Visible = false; } catch { }
            StopPlayback();
            StopMicTest();
            UninstallKeyboardHook();
        }
    }

    internal sealed class LoopStream : WaveStream
    {
        private readonly WaveStream _source;

        public LoopStream(IWaveProvider sourceProvider)
        {
            if (sourceProvider is WaveStream waveStream)
                _source = waveStream;
            else
                throw new ArgumentException("sourceProvider must be a WaveStream or convertible to WaveStream");
        }

        public override WaveFormat WaveFormat => _source.WaveFormat;
        public override long Length => long.MaxValue;
        public override long Position { get => _source.Position; set => _source.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = _source.Read(buffer, offset + total, count - total);
                if (read == 0)
                {
                    _source.Position = 0;
                    continue;
                }
                total += read;
            }
            return total;
        }
    }
}
