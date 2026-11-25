using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PiercerSteamCodingTracker
{
    public class TrackerConfig
    {
        public string SteamAppId = "779260"; // Kode Studio real Steam AppID
        public string TargetProcessName = "KodeStudio"; // Process name to close when IDEs exit
        public int PollIntervalSeconds = 3;
        public int CloseGraceSeconds = 15;
        public bool AutoManageSteam = true;
        public string MonitoredIdes = "Code,Cursor,Windsurf,Antigravity,devenv,rider64,pycharm64,idea64,webstorm64,goland64,clion64,sublime_text,notepad++,nvim,studio64,Zed";
        public long TotalSecondsAllTime = 0;
    }

    public class MainForm : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private readonly Color BgDark = Color.FromArgb(10, 10, 12);
        private readonly Color PanelDark = Color.FromArgb(20, 20, 24);
        private readonly Color CardDark = Color.FromArgb(30, 30, 36);
        private readonly Color BorderDark = Color.FromArgb(42, 42, 50);
        private readonly Color AccentRed = Color.FromArgb(239, 68, 68);
        private readonly Color OkGreen = Color.FromArgb(34, 197, 94);
        private readonly Color TextLight = Color.FromArgb(244, 244, 245);
        private readonly Color MutedText = Color.FromArgb(161, 161, 170);

        private TrackerConfig _cfg = new TrackerConfig();
        private Timer _pollTimer;
        private NotifyIcon _tray;

        private Label _lblStatusBadge;
        private Label _lblActiveIde;
        private Label _lblSessionTime;
        private Label _lblTotalTime;
        private Label _lblSteamState;
        private TextBox _txtAppId;
        private TextBox _txtTargetProc;
        private TextBox _txtIdes;
        private NumericUpDown _numGrace;
        private CheckBox _chkAutoSteam;
        private ListBox _logBox;

        private bool _isCodingNow = false;
        private Process _idleWorkerProc = null;
        private DateTime _sessionStart = DateTime.MinValue;
        private DateTime _lastIdeSeen = DateTime.MinValue;
        private long _sessionSeconds = 0;
        private long _todaySeconds = 0;

        private string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coding_tracker_config.ini"); }
        }

        private string LogCsvPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coding_sessions.csv"); }
        }

        public MainForm()
        {
            Text = "piercer — Steam Coding Time Tracker (Kode Studio Sync)";
            Size = new Size(780, 580);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDark;
            ForeColor = TextLight;
            Font = new Font("Segoe UI", 9.5f);

            LoadConfig();
            BuildUI();
            SetupTray();

            _pollTimer = new Timer();
            _pollTimer.Interval = Math.Max(1, _cfg.PollIntervalSeconds) * 1000;
            _pollTimer.Tick += OnPollTick;
            _pollTimer.Start();

            AddLog("Tracker iniciado. Monitorando IDEs configuradas...");
            OnPollTick(null, EventArgs.Empty);
        }

        private void SetupTray()
        {
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("Abrir Dashboard", (s, e) => { Show(); Activate(); });
            menu.MenuItems.Add("Iniciar App Steam Agora", (s, e) => LaunchSteamApp());
            menu.MenuItems.Add("Fechar App Steam Agora", (s, e) => StopSteamApp());
            menu.MenuItems.Add("-");
            menu.MenuItems.Add("Sair", (s, e) => { FlushSession(); _tray.Visible = false; Application.Exit(); });

            _tray = new NotifyIcon();
            _tray.Text = "piercer Steam Coding Tracker";
            _tray.Icon = SystemIcons.Application;
            _tray.ContextMenu = menu;
            _tray.Visible = true;
            _tray.DoubleClick += (s, e) => { Show(); Activate(); };
        }

        private void BuildUI()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = PanelDark };
            Label title = new Label
            {
                Text = "STEAM CODING TIME TRACKER  |  by piercer",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextLight,
                AutoSize = true,
                Location = new Point(16, 15)
            };
            _lblStatusBadge = new Label
            {
                Text = "AGUARDANDO IDE",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = MutedText,
                BackColor = CardDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(165, 28),
                Location = new Point(580, 13)
            };
            header.Controls.Add(title);
            header.Controls.Add(_lblStatusBadge);
            Controls.Add(header);

            // Stat Cards
            _lblActiveIde = AddStatCard(16, 70, 236, "IDE Ativa no Momento", "Nenhuma");
            _lblSessionTime = AddStatCard(266, 70, 160, "Sessão Atual", "00:00:00");
            _lblTotalTime = AddStatCard(440, 70, 160, "Total Acumulado", FormatDuration(_cfg.TotalSecondsAllTime));
            _lblSteamState = AddStatCard(614, 70, 134, "Steam / App", "Inativo");

            // Config Panel
            Panel cfgPanel = new Panel { Location = new Point(16, 162), Size = new Size(732, 200), BackColor = PanelDark };
            Label lblCfgTitle = new Label { Text = "Configuração de Integração Steam & IDEs", ForeColor = MutedText, Location = new Point(14, 10), AutoSize = true };
            cfgPanel.Controls.Add(lblCfgTitle);

            Label l1 = new Label { Text = "Steam AppID (Kode Studio = 779260):", Location = new Point(14, 42), AutoSize = true };
            _txtAppId = new TextBox { Text = _cfg.SteamAppId, Location = new Point(250, 39), Width = 110, BackColor = CardDark, ForeColor = TextLight, BorderStyle = BorderStyle.FixedSingle };

            Label l2 = new Label { Text = "Processo alvo p/ fechar:", Location = new Point(380, 42), AutoSize = true };
            _txtTargetProc = new TextBox { Text = _cfg.TargetProcessName, Location = new Point(540, 39), Width = 174, BackColor = CardDark, ForeColor = TextLight, BorderStyle = BorderStyle.FixedSingle };

            Label l3 = new Label { Text = "Tolerância p/ fechar (seg):", Location = new Point(14, 76), AutoSize = true };
            _numGrace = new NumericUpDown { Minimum = 3, Maximum = 300, Value = _cfg.CloseGraceSeconds, Location = new Point(250, 74), Width = 110, BackColor = CardDark, ForeColor = TextLight };

            _chkAutoSteam = new CheckBox { Text = "Auto-iniciar e auto-fechar App Steam com a IDE", Checked = _cfg.AutoManageSteam, Location = new Point(380, 76), AutoSize = true };

            Label l4 = new Label { Text = "Processos de IDEs monitorados (separados por vírgula, sem .exe):", Location = new Point(14, 110), AutoSize = true };
            _txtIdes = new TextBox { Text = _cfg.MonitoredIdes, Location = new Point(14, 132), Width = 700, BackColor = CardDark, ForeColor = TextLight, BorderStyle = BorderStyle.FixedSingle };

            Button btnSave = CreateBtn("Salvar Config", 14, 162, 130, AccentRed, (s, e) =>
            {
                _cfg.SteamAppId = _txtAppId.Text.Trim();
                _cfg.TargetProcessName = _txtTargetProc.Text.Trim();
                _cfg.CloseGraceSeconds = (int)_numGrace.Value;
                _cfg.AutoManageSteam = _chkAutoSteam.Checked;
                _cfg.MonitoredIdes = _txtIdes.Text.Trim();
                SaveConfig();
                AddLog("Configurações salvas.");
            });

            Button btnLaunch = CreateBtn("Simular Agora", 152, 162, 115, CardDark, (s, e) => LaunchSteamApp());
            Button btnInstall = CreateBtn("Loja Kode Studio", 275, 162, 145, CardDark, (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://store.steampowered.com/app/779260/Kode_Studio/") { UseShellExecute = true }); } catch { }
            });
            Button btnStop = CreateBtn("Parar Simulação", 428, 162, 125, CardDark, (s, e) => StopSteamApp());
            Button btnHide = CreateBtn("Minimizar Tray", 564, 162, 150, CardDark, (s, e) => Hide());

            cfgPanel.Controls.AddRange(new Control[] { l1, _txtAppId, l2, _txtTargetProc, l3, _numGrace, _chkAutoSteam, l4, _txtIdes, btnSave, btnLaunch, btnInstall, btnStop, btnHide });
            Controls.Add(cfgPanel);

            // Log Box
            Label lblLog = new Label { Text = "Log de Atividade em Tempo Real", ForeColor = MutedText, Location = new Point(16, 374), AutoSize = true };
            _logBox = new ListBox
            {
                Location = new Point(16, 396),
                Size = new Size(732, 132),
                BackColor = PanelDark,
                ForeColor = TextLight,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lblLog);
            Controls.Add(_logBox);
        }

        private Label AddStatCard(int x, int y, int w, string caption, string initial)
        {
            Panel card = new Panel { Location = new Point(x, y), Size = new Size(w, 78), BackColor = PanelDark };
            Label cap = new Label { Text = caption, ForeColor = MutedText, Location = new Point(12, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            Label val = new Label { Text = initial, ForeColor = TextLight, Location = new Point(12, 34), Size = new Size(w - 24, 30), Font = new Font("Segoe UI", 12f, FontStyle.Bold) };
            card.Controls.Add(cap);
            card.Controls.Add(val);
            Controls.Add(card);
            return val;
        }

        private Button CreateBtn(string text, int x, int y, int w, Color bg, EventHandler onClick)
        {
            Button b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 28),
                BackColor = bg,
                ForeColor = TextLight,
                FlatStyle = FlatStyle.Flat
            };
            b.FlatAppearance.BorderColor = BorderDark;
            b.Click += onClick;
            return b;
        }

        private void OnPollTick(object sender, EventArgs e)
        {
            List<string> active = DetectActiveIdes();
            bool steamRunning = Process.GetProcessesByName("steam").Length > 0;
            bool workerRunning = (_idleWorkerProc != null && !_idleWorkerProc.HasExited);

            if (active.Count > 0)
            {
                _lastIdeSeen = DateTime.Now;
                string activeLabel = string.Join(", ", active.ToArray());
                _lblActiveIde.Text = activeLabel;
                _lblActiveIde.ForeColor = OkGreen;

                if (!_isCodingNow)
                {
                    _isCodingNow = true;
                    _sessionStart = DateTime.Now;
                    _sessionSeconds = 0;
                    _lblStatusBadge.Text = "CODANDO (ATIVO)";
                    _lblStatusBadge.ForeColor = OkGreen;
                    AddLog("IDE detectada: " + activeLabel);

                    if (_cfg.AutoManageSteam && steamRunning && !workerRunning)
                    {
                        LaunchSteamApp();
                        workerRunning = (_idleWorkerProc != null && !_idleWorkerProc.HasExited);
                    }
                }
                else
                {
                    long delta = Math.Max(1, _cfg.PollIntervalSeconds);
                    _sessionSeconds += delta;
                    _todaySeconds += delta;
                    _cfg.TotalSecondsAllTime += delta;
                }

                _lblSessionTime.Text = FormatDuration(_sessionSeconds);
                _lblTotalTime.Text = FormatDuration(_cfg.TotalSecondsAllTime);
            }
            else
            {
                _lblActiveIde.Text = "Nenhuma";
                _lblActiveIde.ForeColor = MutedText;

                if (_isCodingNow)
                {
                    double elapsedSinceLast = (DateTime.Now - _lastIdeSeen).TotalSeconds;
                    if (elapsedSinceLast >= _cfg.CloseGraceSeconds)
                    {
                        FlushSession();
                        _isCodingNow = false;
                        _lblStatusBadge.Text = "AGUARDANDO IDE";
                        _lblStatusBadge.ForeColor = MutedText;
                        AddLog("Nenhuma IDE ativa após tolerância. Sessão encerrada.");

                        if (_cfg.AutoManageSteam && workerRunning)
                        {
                            StopSteamApp();
                            workerRunning = false;
                        }
                    }
                    else
                    {
                        _lblStatusBadge.Text = string.Format("PAUSA ({0}s)", (int)(_cfg.CloseGraceSeconds - elapsedSinceLast));
                    }
                }
            }

            if (!steamRunning)
            {
                _lblSteamState.Text = "Steam Offline";
                _lblSteamState.ForeColor = MutedText;
            }
            else if (workerRunning)
            {
                _lblSteamState.Text = "Simulando App";
                _lblSteamState.ForeColor = OkGreen;
            }
            else
            {
                _lblSteamState.Text = "Steam Online";
                _lblSteamState.ForeColor = TextLight;
            }
        }

        private List<string> DetectActiveIdes()
        {
            List<string> found = new List<string>();
            string[] names = _cfg.MonitoredIdes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string raw in names)
            {
                string proc = raw.Trim();
                if (string.IsNullOrEmpty(proc)) continue;
                try
                {
                    if (Process.GetProcessesByName(proc).Length > 0)
                    {
                        found.Add(proc);
                    }
                }
                catch { }
            }
            return found;
        }

        private void LaunchSteamApp()
        {
            if (string.IsNullOrEmpty(_cfg.SteamAppId)) return;
            try
            {
                if (_idleWorkerProc != null && !_idleWorkerProc.HasExited) return;
                string workerExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "piercer_steam_idle_worker.exe");
                if (!File.Exists(workerExe))
                {
                    AddLog("Worker nao encontrado: piercer_steam_idle_worker.exe");
                    return;
                }
                int myPid = Process.GetCurrentProcess().Id;
                ProcessStartInfo psi = new ProcessStartInfo(workerExe, string.Format("{0} {1}", _cfg.SteamAppId, myPid));
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _idleWorkerProc = Process.Start(psi);
                AddLog("Simulando Steam AppID " + _cfg.SteamAppId + " em segundo plano (sem abrir o app).");
            }
            catch (Exception ex)
            {
                AddLog("Erro ao iniciar worker Steam: " + ex.Message);
            }
        }

        private void StopSteamApp()
        {
            try
            {
                if (_idleWorkerProc != null && !_idleWorkerProc.HasExited)
                {
                    _idleWorkerProc.Kill();
                    _idleWorkerProc.Dispose();
                    _idleWorkerProc = null;
                    AddLog("Simulacao na Steam encerrada.");
                }
            }
            catch (Exception ex)
            {
                AddLog("Erro ao parar worker: " + ex.Message);
            }
        }

        private void FlushSession()
        {
            if (_sessionSeconds <= 0) return;
            try
            {
                bool exists = File.Exists(LogCsvPath);
                using (StreamWriter sw = new StreamWriter(LogCsvPath, true, Encoding.UTF8))
                {
                    if (!exists) sw.WriteLine("Start,End,DurationSeconds");
                    sw.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss},{1:yyyy-MM-dd HH:mm:ss},{2}", _sessionStart, DateTime.Now, _sessionSeconds));
                }
                SaveConfig();
            }
            catch { }
            _sessionSeconds = 0;
        }

        private void AddLog(string msg)
        {
            if (_logBox == null) return;
            string line = string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, msg);
            _logBox.Items.Insert(0, line);
            while (_logBox.Items.Count > 100) _logBox.Items.RemoveAt(_logBox.Items.Count - 1);
        }

        private static string FormatDuration(long totalSecs)
        {
            TimeSpan ts = TimeSpan.FromSeconds(totalSecs);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        }

        private void SaveConfig()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SteamAppId=" + _cfg.SteamAppId);
                sb.AppendLine("TargetProcessName=" + _cfg.TargetProcessName);
                sb.AppendLine("CloseGraceSeconds=" + _cfg.CloseGraceSeconds);
                sb.AppendLine("AutoManageSteam=" + _cfg.AutoManageSteam);
                sb.AppendLine("MonitoredIdes=" + _cfg.MonitoredIdes);
                sb.AppendLine("TotalSecondsAllTime=" + _cfg.TotalSecondsAllTime);
                File.WriteAllText(ConfigPath, sb.ToString());
            }
            catch { }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    string k = parts[0].Trim();
                    string v = parts[1].Trim();
                    if (k == "SteamAppId") _cfg.SteamAppId = v;
                    else if (k == "TargetProcessName") _cfg.TargetProcessName = v;
                    else if (k == "CloseGraceSeconds") _cfg.CloseGraceSeconds = int.Parse(v);
                    else if (k == "AutoManageSteam") _cfg.AutoManageSteam = bool.Parse(v);
                    else if (k == "MonitoredIdes") _cfg.MonitoredIdes = v;
                    else if (k == "TotalSecondsAllTime") _cfg.TotalSecondsAllTime = long.Parse(v);
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            FlushSession();
            StopSteamApp();
            if (_tray != null) _tray.Visible = false;
            base.OnFormClosing(e);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
