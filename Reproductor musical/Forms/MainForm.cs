using System;
using System.Drawing;
using System.Windows.Forms;

namespace Reproductor_musical.Forms
{
    public partial class MainForm : Form
    {
        private AudioEngine _audio;
        private Visualizer _visualizer;
        private Bitmap _bufferA;
        private Bitmap _bufferB;
        private bool _useBufferA;
        private System.Windows.Forms.Timer _renderTimer;
        private bool _isDraggingProgress;

        // Controles UI
        private PictureBox _canvas;
        private Button _btnLoad, _btnPlay, _btnPause, _btnStop;
        private TrackBar _tbVolume, _tbProgress;
        private Label _lblTime, _lblTitle;
        private ComboBox _cmbMode;
        private Panel _controlPanel;

        public MainForm()
        {
            InitializeComponents();
            _audio = new AudioEngine();
            _visualizer = new Visualizer();
            _audio.FftDataAvailable += (s, data) => _visualizer.UpdateSpectrum(data);

            // Timer de renderizado a ~60 FPS
            _renderTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();
        }

        private void InitializeComponents()
        {
            this.Text = "🎵 Music Visualizer";
        this.Size = new Size(1200, 800);
        this.BackColor = Color.Black;
        this.MinimumSize = new Size(800, 600);

        // Canvas de animación (área principal)
        _canvas = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        // Panel de controles en la parte inferior
        _controlPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            BackColor = Color.FromArgb(20, 20, 30),
            Padding = new Padding(10)
        };

        // Botones
        _btnLoad = CreateButton("📂 Cargar", Color.FromArgb(60, 60, 80));
        _btnPlay = CreateButton("▶ Play", Color.FromArgb(0, 120, 60));
        _btnPause = CreateButton("⏸ Pause", Color.FromArgb(120, 80, 0));
        _btnStop = CreateButton("⏹ Stop", Color.FromArgb(120, 0, 0));

        _btnLoad.Click += BtnLoad_Click;
        _btnPlay.Click += (s, e) => _audio.Play();
        _btnPause.Click += (s, e) => _audio.Pause();
        _btnStop.Click += (s, e) => { _audio.Stop(); _tbProgress.Value = 0; };

        // ComboBox de modos
        _cmbMode = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 160,
            Height = 30,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(40, 40, 60)
        };
        _cmbMode.Items.AddRange(new[] { "Espectro de Barras", "Partículas", "Onda Circular", "Geometría" });
        _cmbMode.SelectedIndex = 0;
        _cmbMode.SelectedIndexChanged += (s, e) =>
            _visualizer.Mode = (VisualizationMode)_cmbMode.SelectedIndex;

        // Barra de progreso
        _tbProgress = new TrackBar { Minimum = 0, Maximum = 1000, Width = 600, TickStyle = TickStyle.None };
        _tbProgress.MouseDown += (s, e) => _isDraggingProgress = true;
        _tbProgress.MouseUp += (s, e) => _isDraggingProgress = false;
        _tbProgress.Scroll += (s, e) =>
        {
            if (_audio.TotalTime.TotalSeconds > 0)
                _audio.Seek(_tbProgress.Value / 1000.0 * _audio.TotalTime.TotalSeconds);
        };

        // Volumen
        var lblVol = new Label { Text = "🔊", ForeColor = Color.White, AutoSize = true };
        _tbVolume = new TrackBar { Minimum = 0, Maximum = 100, Value = 80, Width = 120, TickStyle = TickStyle.None };
        _tbVolume.Scroll += (s, e) => _audio.SetVolume(_tbVolume.Value / 100f);

        // Etiqueta de tiempo
        _lblTime = new Label { Text = "00:00 / 00:00", ForeColor = Color.White, AutoSize = true };
        _lblTitle = new Label { Text = "Sin archivo cargado", ForeColor = Color.Cyan, AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

        // Layout del panel (FlowLayout por simplicidad)
        var flow1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, BackColor = Color.Transparent, Padding = new Padding(5, 8, 5, 0) };
        flow1.Controls.AddRange(new Control[] { _btnLoad, _btnPlay, _btnPause, _btnStop, _cmbMode, _lblTitle });

        var flow2 = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(5, 0, 5, 5) };
        flow2.Controls.AddRange(new Control[] { _tbProgress, lblVol, _tbVolume, _lblTime });

        _controlPanel.Controls.Add(flow2);
        _controlPanel.Controls.Add(flow1);

        this.Controls.Add(_canvas);
        this.Controls.Add(_controlPanel);
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

    private void BtnLoad_Click(object sender, EventArgs e)
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Filter = "Audio|*.mp3;*.wav;*.flac;*.ogg;*.aac|Todos|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _audio.Load(dlg.FileName);
                _audio.SetVolume(_tbVolume.Value / 100f);
                _lblTitle.Text = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                _audio.Play();
            }
        }
    }

    // Loop de renderizado con doble buffer
    private void OnRenderTick(object sender, EventArgs e)
    {
        int w = _canvas.Width, h = _canvas.Height;
        if (w <= 0 || h <= 0) return;

        Bitmap current = _useBufferA ? _bufferA : _bufferB;
        if (current == null || current.Width != w || current.Height != h)
        {
            current?.Dispose();
            current = new Bitmap(w, h);
            if (_useBufferA) _bufferA = current; else _bufferB = current;
        }

        using (var g = Graphics.FromImage(current))
            _visualizer.Render(g, w, h);

        var old = (Bitmap)_canvas.Image;
        _canvas.Image = current;
        _useBufferA = !_useBufferA;
        if (old != null && old != current)
        {
            if (old == _bufferA) _bufferA = null;
            if (old == _bufferB) _bufferB = null;
            old.Dispose();
        }

        if (!_isDraggingProgress && _audio.TotalTime.TotalSeconds > 0)
        {
            double progress = _audio.CurrentTime.TotalSeconds / _audio.TotalTime.TotalSeconds;
            _tbProgress.Value = (int)(progress * 1000);
            _lblTime.Text = $"{_audio.CurrentTime:mm\\:ss} / {_audio.TotalTime:mm\\:ss}";
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _renderTimer.Stop();
        _audio.Dispose();
        _bufferA?.Dispose();
        _bufferB?.Dispose();
        base.OnFormClosed(e);
    }
}
}