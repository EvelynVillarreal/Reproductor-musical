using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using Reproductor_musical.Controllers;
using Reproductor_musical.Visuals;

namespace Reproductor_musical.Forms
{
    public partial class MainForm : Form
    {
        private readonly PlayerController _controller;
        private PictureBox _canvas;
        private Panel _controlPanel;

        // Controles de reproducción y progreso
        private Label _lblTitle, _lblCurrentTime, _lblTotalTime;
        private Button _btnPrev, _btnPlayPause, _btnNext;
        private PictureBox _pbProgress, _pbVolume;
        private ComboBox _cmbMode;
        private Label _lblVolIcon;

        // NUEVOS CONTROLES: Biblioteca en Combo y Botón dedicado
        private ComboBox _cmbSongs;
        private Button _btnAddSong;

        private Timer _renderTimer;
        private Bitmap _bufferA, _bufferB;
        private bool _useBufferA;
        private bool _isPlaying = false;
        private float _currentVolume = 0.8f;

        private bool _isDraggingProgress = false;
        private bool _isDraggingVolume = false;

        // Sistema de Playlist
        private List<string> _playlist = new List<string>();
        private int _currentSongIndex = -1;
        private string _libraryPath;

        // Bandera de seguridad para evitar disparos infinitos entre eventos
        private bool _isChangingSongFromCode = false;

        public MainForm(PlayerController controller)
        {
            _controller = controller;

            _libraryPath = Path.Combine(Application.StartupPath, "MusicLibrary");
            if (!Directory.Exists(_libraryPath))
            {
                Directory.CreateDirectory(_libraryPath);
            }

            InitializeSpotifyComponents();
            WireControllerEvents();
            LoadPlaylistFromFolder();
            StartRenderLoop();
        }

        private void LoadPlaylistFromFolder()
        {
            _isChangingSongFromCode = true; // Bloqueamos temporalmente el evento del ComboBox

            _playlist.Clear();
            _cmbSongs.Items.Clear();

            if (Directory.Exists(_libraryPath))
            {
                string[] files = Directory.GetFiles(_libraryPath);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".mp3" || ext == ".wav" || ext == ".flac" || ext == ".ogg" || ext == ".aac")
                    {
                        _playlist.Add(file);
                        _cmbSongs.Items.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
            }

            // Si hay música guardada, pre-seleccionamos la primera pista
            if (_playlist.Count > 0)
            {
                if (_currentSongIndex == -1 || _currentSongIndex >= _playlist.Count)
                    _currentSongIndex = 0;

                _cmbSongs.SelectedIndex = _currentSongIndex;
                _lblTitle.Text = "Pista actual: " + Path.GetFileNameWithoutExtension(_playlist[_currentSongIndex]);
            }
            else
            {
                _currentSongIndex = -1;
                _lblTitle.Text = "Biblioteca vacía";
            }

            _isChangingSongFromCode = false; // Liberamos el evento
        }

        private void InitializeSpotifyComponents()
        {
            this.Text = "Music Visualizer";
            this.Size = new Size(1200, 800);
            this.BackColor = Color.Black;
            this.MinimumSize = new Size(900, 600);

            _canvas = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Black };

            _controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                BackColor = Color.FromArgb(2, 0, 30)
            };

            // --- SECCIÓN IZQUIERDA REESTRUCTURADA ---
            _lblTitle = new Label
            {
                Text = "Biblioteca vacía",
                ForeColor = Color.Cyan,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(260, 20)
            };

            _cmbSongs = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 260,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            _cmbSongs.SelectedIndexChanged += (s, e) =>
            {
                if (_isChangingSongFromCode) return; // Si el cambio viene de los botones ⏮/⏭, no hacemos nada
                if (_cmbSongs.SelectedIndex >= 0)
                {
                    _currentSongIndex = _cmbSongs.SelectedIndex;
                    LoadCurrentSongToController();
                    ForcePlay();
                }
            };

            _btnAddSong = new Button
            {
                Text = "➕ Añadir Música",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 70),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 28),
                Cursor = Cursors.Hand
            };
            _btnAddSong.FlatAppearance.BorderSize = 0;
            _btnAddSong.Click += (s, e) => OnAddSongClicked();


            // --- SECCIÓN CENTRAL ---
            _btnPrev = CreateIconButton("⏮", 16);
            _btnPlayPause = CreateIconButton("▶", 24);
            _btnNext = CreateIconButton("⏭", 16);

            _btnPrev.Click += (s, e) => PlayPreviousSong();
            _btnPlayPause.Click += (s, e) => TogglePlayPause();
            _btnNext.Click += (s, e) => PlayNextSong();

            _lblCurrentTime = new Label { Text = "0:00", ForeColor = Color.Gray, AutoSize = true, Font = new Font("Segoe UI", 8) };
            _lblTotalTime = new Label { Text = "0:00", ForeColor = Color.Gray, AutoSize = true, Font = new Font("Segoe UI", 8) };

            _pbProgress = new PictureBox { Size = new Size(440, 20), Cursor = Cursors.Hand };
            _pbProgress.Paint += PbProgress_Paint;
            _pbProgress.MouseDown += (s, e) => { _isDraggingProgress = true; UpdateProgress(e.X); };
            _pbProgress.MouseMove += (s, e) => { if (_isDraggingProgress) UpdateProgress(e.X); };
            _pbProgress.MouseUp += (s, e) => { _isDraggingProgress = false; };

            // --- SECCIÓN DERECHA ---
            _cmbMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            _cmbMode.Items.AddRange(new[] { "Espectro de Barras", "Particulas", "Onda Circular", "Geometria", "Onda Rellena", "Osciloscopio" });
            _cmbMode.SelectedIndex = 0;
            _cmbMode.SelectedIndexChanged += (s, e) => _controller.Visualizer.Mode = (VisualizationMode)_cmbMode.SelectedIndex;

            _lblVolIcon = new Label { Text = "🔊", ForeColor = Color.Gray, AutoSize = true, Font = new Font("Segoe UI", 12) };

            _pbVolume = new PictureBox { Size = new Size(100, 20), Cursor = Cursors.Hand };
            _pbVolume.Paint += PbVolume_Paint;
            _pbVolume.MouseDown += (s, e) => { _isDraggingVolume = true; UpdateVolume(e.X); };
            _pbVolume.MouseMove += (s, e) => { if (_isDraggingVolume) UpdateVolume(e.X); };
            _pbVolume.MouseUp += (s, e) => { _isDraggingVolume = false; };

            // --- REPOSICIONAMIENTO COMPLETO ---
            this.Resize += (s, e) =>
            {
                int cx = this.Width / 2;

                // Centro (Botones arriba, barra abajo bien separada)
                _btnPrev.Location = new Point(cx - 75, 20);
                _btnPlayPause.Location = new Point(cx - 25, 15);
                _btnNext.Location = new Point(cx + 35, 20);

                _lblCurrentTime.Location = new Point(cx - 260, 82);
                _lblTotalTime.Location = new Point(cx + 230, 82);
                _pbProgress.Location = new Point(cx - 220, 80);

                // Izquierda apilada verticalmente de forma limpia
                _lblTitle.Location = new Point(20, 15);
                _cmbSongs.Location = new Point(20, 45);
                _btnAddSong.Location = new Point(20, 80);

                // Derecha ordenada
                _cmbMode.Location = new Point(this.Width - 180, 30);
                _lblVolIcon.Location = new Point(this.Width - 170, 75);
                _pbVolume.Location = new Point(this.Width - 140, 78);
            };

            // Inyectar controles al contenedor principal
            _controlPanel.Controls.Add(_lblTitle);
            _controlPanel.Controls.Add(_cmbSongs);
            _controlPanel.Controls.Add(_btnAddSong);
            _controlPanel.Controls.Add(_btnPrev);
            _controlPanel.Controls.Add(_btnPlayPause);
            _controlPanel.Controls.Add(_btnNext);
            _controlPanel.Controls.Add(_lblCurrentTime);
            _controlPanel.Controls.Add(_pbProgress);
            _controlPanel.Controls.Add(_lblTotalTime);
            _controlPanel.Controls.Add(_cmbMode);
            _controlPanel.Controls.Add(_lblVolIcon);
            _controlPanel.Controls.Add(_pbVolume);

            this.Controls.Add(_canvas);
            this.Controls.Add(_controlPanel);

            this.OnResize(EventArgs.Empty);
        }

        private Button CreateIconButton(string icon, int fontSize)
        {
            var btn = new Button
            {
                Text = icon,
                Font = new Font("Segoe UI", fontSize),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 60);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 80);
            return btn;
        }

        private void PbProgress_Paint(object sender, PaintEventArgs e)
        {
            float percentage = _controller.CurrentSong.TotalTime.TotalSeconds > 0 ? (float)_controller.GetProgress() : 0;
            DrawFlatBar(e.Graphics, _pbProgress.Width, _pbProgress.Height, percentage, Color.White, Color.FromArgb(80, 80, 80));
        }

        private void PbVolume_Paint(object sender, PaintEventArgs e)
        {
            DrawFlatBar(e.Graphics, _pbVolume.Width, _pbVolume.Height, _currentVolume, Color.FromArgb(30, 215, 96), Color.FromArgb(80, 80, 80));
        }

        private void DrawFlatBar(Graphics g, int width, int height, float percentage, Color fg, Color bg)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int barHeight = 4;
            int y = (height - barHeight) / 2;

            g.FillRectangle(new SolidBrush(bg), 0, y, width, barHeight);
            int fillWidth = (int)(width * percentage);
            g.FillRectangle(new SolidBrush(fg), 0, y, fillWidth, barHeight);

            int circleX = fillWidth;
            circleX = Math.Max(5, Math.Min(width - 5, circleX));
            g.FillEllipse(new SolidBrush(Color.White), circleX - 5, y - 3, 10, 10);
        }

        private void UpdateProgress(int mouseX)
        {
            if (_controller.CurrentSong.TotalTime.TotalSeconds > 0)
            {
                float percentage = Math.Max(0f, Math.Min(1f, (float)mouseX / _pbProgress.Width));
                _controller.Seek(percentage * _controller.CurrentSong.TotalTime.TotalSeconds);
                _pbProgress.Invalidate();
            }
        }

        private void UpdateVolume(int mouseX)
        {
            _currentVolume = Math.Max(0f, Math.Min(1f, (float)mouseX / _pbVolume.Width));
            _controller.SetVolume(_currentVolume);
            _pbVolume.Invalidate();
        }

        private void TogglePlayPause()
        {
            if (_playlist.Count == 0) return;

            if (_controller.CurrentSong.TotalTime.TotalSeconds == 0 && _currentSongIndex >= 0)
            {
                LoadCurrentSongToController();
            }

            if (_isPlaying)
            {
                _controller.Pause();
                _btnPlayPause.Text = "▶";
            }
            else
            {
                _controller.Play();
                _btnPlayPause.Text = "⏸";
            }
            _isPlaying = !_isPlaying;
        }

        private void PlayNextSong()
        {
            if (_playlist.Count == 0) return;
            _currentSongIndex++;
            if (_currentSongIndex >= _playlist.Count) _currentSongIndex = 0;
            LoadCurrentSongToController();
            ForcePlay();
        }

        private void PlayPreviousSong()
        {
            if (_playlist.Count == 0) return;
            _currentSongIndex--;
            if (_currentSongIndex < 0) _currentSongIndex = _playlist.Count - 1;
            LoadCurrentSongToController();
            ForcePlay();
        }

        private void LoadCurrentSongToController()
        {
            _controller.LoadSong(_playlist[_currentSongIndex]);

            // Sincronizamos el ComboBox visual sin disparar el evento recursivo
            _isChangingSongFromCode = true;
            _cmbSongs.SelectedIndex = _currentSongIndex;
            _lblTitle.Text = "Pista actual: " + Path.GetFileNameWithoutExtension(_playlist[_currentSongIndex]);
            _isChangingSongFromCode = false;
        }

        private void ForcePlay()
        {
            _isPlaying = true;
            _btnPlayPause.Text = "⏸";
            _controller.Play();
        }

        private void OnAddSongClicked()
        {
            using (var dlg = new OpenFileDialog { Filter = "Audio|*.mp3;*.wav;*.flac;*.ogg;*.aac|Todos|*.*", Multiselect = true })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    foreach (string sourceFile in dlg.FileNames)
                    {
                        string fileName = Path.GetFileName(sourceFile);
                        string destFile = Path.Combine(_libraryPath, fileName);

                        if (!File.Exists(destFile))
                        {
                            File.Copy(sourceFile, destFile);
                        }
                    }

                    // Recargamos la lista física y visual
                    LoadPlaylistFromFolder();
                }
            }
        }

        private void WireControllerEvents()
        {
            _controller.SongLoaded += () =>
            {
                _controller.SetVolume(_currentVolume);
            };
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
            if (_controller.CurrentSong.TotalTime.TotalSeconds > 0 && !_isDraggingProgress)
            {
                _lblCurrentTime.Text = _controller.CurrentSong.CurrentTime.ToString(@"m\:ss");
                _lblTotalTime.Text = _controller.CurrentSong.TotalTime.ToString(@"m\:ss");
                _pbProgress.Invalidate();

                if (_controller.GetProgress() >= 0.999f)
                {
                    PlayNextSong();
                }
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