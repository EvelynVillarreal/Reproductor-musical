using System;
using System.Drawing;
using System.Windows.Forms;
using Reproductor_musical.Controllers;
using Reproductor_musical.Visuals;

namespace Reproductor_musical.Forms
{
    public partial class MainForm : Form
    {
        private readonly PlayerController _controller;
        private PictureBox _canvas;
        private Button _btnLoad, _btnPlay, _btnPause, _btnStop;
        private TrackBar _tbVolume, _tbProgress;
        private Label _lblTime, _lblTitle;
        private ComboBox _cmbMode;
        private Panel _controlPanel;
        private Timer _renderTimer;
        private Bitmap _bufferA;
        private Bitmap _bufferB;
        private bool _useBufferA;
        private bool _isDraggingProgress;

        public MainForm(PlayerController controller)
        {
            _controller = controller;
            InitializeComponents();
            WireControllerEvents();
            StartRenderLoop();
        }

        private void InitializeComponents()
        {
            this.Text = "Music Visualizer";
            this.Size = new Size(1200, 800);
            this.BackColor = Color.Black;
            this.MinimumSize = new Size(800, 600);

            _canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            _controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Color.FromArgb(20, 20, 30),
                Padding = new Padding(10)
            };

            _btnLoad = CreateButton("Cargar", Color.FromArgb(60, 60, 80));
            _btnPlay = CreateButton("Play", Color.FromArgb(0, 120, 60));
            _btnPause = CreateButton("Pause", Color.FromArgb(120, 80, 0));
            _btnStop = CreateButton("Stop", Color.FromArgb(120, 0, 0));

            _btnLoad.Click += (s, e) => OnLoadClicked();
            _btnPlay.Click += (s, e) => _controller.Play();
            _btnPause.Click += (s, e) => _controller.Pause();
            _btnStop.Click += (s, e) => _controller.Stop();

            _cmbMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Height = 30,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 60)
            };
            _cmbMode.Items.AddRange(new[] { "Espectro de Barras", "Particulas", "Onda Circular", "Geometria", "Onda Rellenada" });
            _cmbMode.SelectedIndex = 0;
            _cmbMode.SelectedIndexChanged += (s, e) =>
                _controller.Visualizer.Mode = (VisualizationMode)_cmbMode.SelectedIndex;

            _tbProgress = new TrackBar { Minimum = 0, Maximum = 1000, Width = 600, TickStyle = TickStyle.None };
            _tbProgress.MouseDown += (s, e) => _isDraggingProgress = true;
            _tbProgress.MouseUp += (s, e) => _isDraggingProgress = false;
            _tbProgress.Scroll += (s, e) =>
                _controller.Seek(_tbProgress.Value / 1000.0 * _controller.CurrentSong.TotalTime.TotalSeconds);

            var lblVol = new Label { Text = "Vol", ForeColor = Color.White, AutoSize = true };
            _tbVolume = new TrackBar { Minimum = 0, Maximum = 100, Value = 80, Width = 120, TickStyle = TickStyle.None };
            _tbVolume.Scroll += (s, e) => _controller.SetVolume(_tbVolume.Value / 100f);

            _lblTime = new Label { Text = "00:00 / 00:00", ForeColor = Color.White, AutoSize = true };
            _lblTitle = new Label { Text = "Sin archivo cargado", ForeColor = Color.Cyan, AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            var flow1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, BackColor = Color.Transparent, Padding = new Padding(5, 8, 5, 0) };
            flow1.Controls.AddRange(new Control[] { _btnLoad, _btnPlay, _btnPause, _btnStop, _cmbMode, _lblTitle });

            var flow2 = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(5, 0, 5, 5) };
            flow2.Controls.AddRange(new Control[] { _tbProgress, lblVol, _tbVolume, _lblTime });

            _controlPanel.Controls.Add(flow2);
            _controlPanel.Controls.Add(flow1);

            this.Controls.Add(_canvas);
            this.Controls.Add(_controlPanel);
        }

        private void WireControllerEvents()
        {
            _controller.SongLoaded += OnSongLoaded;
            _controller.PlaybackChanged += OnPlaybackChanged;
        }

        private Button CreateButton(string text, Color color) => new Button
        {
            Text = text,
            Width = 90,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            FlatAppearance = { BorderColor = Color.FromArgb(80, 80, 100) }
        };

        private void OnLoadClicked()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Audio|*.mp3;*.wav;*.flac;*.ogg;*.aac|Todos|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _controller.LoadSong(dlg.FileName);
                    _tbVolume.Value = (int)(_controller.CurrentSong.Volume * 100);
                }
            }
        }

        private void OnSongLoaded()
        {
            _lblTitle.Text = _controller.CurrentSong.Title;
        }

        private void OnPlaybackChanged()
        {
            var song = _controller.CurrentSong;
            if (!song.IsLoaded)
            {
                _lblTitle.Text = "Sin archivo cargado";
                _tbProgress.Value = 0;
                _lblTime.Text = "00:00 / 00:00";
            }
        }

        private void StartRenderLoop()
        {
            _renderTimer = new Timer { Interval = 16 };
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            int ancho = _canvas.Width, alto = _canvas.Height;
            if (ancho <= 0 || alto <= 0) return;

            Bitmap bufferActual = _useBufferA ? _bufferA : _bufferB;
            if (bufferActual == null || bufferActual.Width != ancho || bufferActual.Height != alto)
            {
                if (_useBufferA) { _bufferA?.Dispose(); _bufferA = new Bitmap(ancho, alto); bufferActual = _bufferA; }
                else { _bufferB?.Dispose(); _bufferB = new Bitmap(ancho, alto); bufferActual = _bufferB; }
            }

            using (var graficos = Graphics.FromImage(bufferActual))
            {
                _controller.Visualizer.Render(graficos, ancho, alto);
            }

            _canvas.Image = bufferActual;
            _useBufferA = !_useBufferA;

            _controller.UpdatePlaybackInfo();
            if (!_isDraggingProgress && _controller.CurrentSong.TotalTime.TotalSeconds > 0)
            {
                int progress = (int)(_controller.GetProgress() * 1000);
                _tbProgress.Value = Math.Min(progress, _tbProgress.Maximum);
                _lblTime.Text = $"{_controller.CurrentSong.CurrentTime:mm\\:ss} / {_controller.CurrentSong.TotalTime:mm\\:ss}";
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _renderTimer.Stop();
            _controller.Dispose();
            _bufferA?.Dispose();
            _bufferB?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
