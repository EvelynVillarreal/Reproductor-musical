using System;
using Reproductor_musical.Core;
using Reproductor_musical.Models;
using Reproductor_musical.Visuals;

namespace Reproductor_musical.Controllers
{
    public class PlayerController : IDisposable
    {
        private readonly AudioEngine _audio;
        private readonly Visualizer _visualizer;
        private readonly SongModel _song;

        public AudioEngine Audio => _audio;
        public Visualizer Visualizer => _visualizer;
        public SongModel CurrentSong => _song;

        public event Action SongLoaded;
        public event Action PlaybackChanged;
        public event Action<float[]> SpectrumUpdated;

        public PlayerController()
        {
            _audio = new AudioEngine();
            _visualizer = new Visualizer();
            _song = new SongModel();

            _audio.FftDataAvailable += OnFftDataAvailable;
        }

        private void OnFftDataAvailable(object sender, float[] spectrum)
        {
            _visualizer.UpdateSpectrum(spectrum);
            SpectrumUpdated?.Invoke(spectrum);
        }

        public void LoadSong(string filePath)
        {
            _audio.Load(filePath);
            _audio.SetVolume(_song.Volume);
            _song.FilePath = filePath;
            _song.Title = System.IO.Path.GetFileNameWithoutExtension(filePath);
            _song.TotalTime = _audio.TotalTime;

            SongLoaded?.Invoke();

            _audio.Play();
            _song.IsPlaying = true;
            PlaybackChanged?.Invoke();
        }

        public void Play()
        {
            _audio.Play();
            _song.IsPlaying = true;
            PlaybackChanged?.Invoke();
        }

        public void Pause()
        {
            _audio.Pause();
            _song.IsPlaying = false;
            PlaybackChanged?.Invoke();
        }

        public void Stop()
        {
            _audio.Stop();
            _song.IsPlaying = false;
            _song.CurrentTime = TimeSpan.Zero;
            _song.FilePath = null;
            _song.Title = "Sin archivo cargado";
            PlaybackChanged?.Invoke();
        }

        public void SetVolume(float vol)
        {
            _song.Volume = vol;
            _audio.SetVolume(vol);
        }

        public void Seek(double seconds)
        {
            _audio.Seek(seconds);
        }

        public void UpdatePlaybackInfo()
        {
            if (_song.IsLoaded)
            {
                _song.CurrentTime = _audio.CurrentTime;
                _song.TotalTime = _audio.TotalTime;
            }
        }

        public double GetProgress()
        {
            if (_song.TotalTime.TotalSeconds <= 0) return 0;
            return _song.CurrentTime.TotalSeconds / _song.TotalTime.TotalSeconds;
        }

        public void Dispose()
        {
            _audio.Dispose();
        }
    }
}
