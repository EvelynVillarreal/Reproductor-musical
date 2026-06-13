using NAudio.Wave;
using System;

namespace Reproductor_musical.Core
{
    public class AudioEngine : IDisposable
    {
        private WaveOutEvent _waveOut;
        private AudioFileReader _audioFile;
        private SampleAggregator _aggregator;

        public float[] SpectrumData { get; private set; } = new float[256];
        public float[] WaveformData { get; private set; } = new float[1024];
        public float Volume => _audioFile?.Volume ?? 0f;
        public TimeSpan CurrentTime => _audioFile?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TotalTime => _audioFile?.TotalTime ?? TimeSpan.Zero;
        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

        public event EventHandler<float[]> FftDataAvailable;

        public void Load(string filePath)
        {
            Stop();
            _audioFile = new AudioFileReader(filePath);
            _aggregator = new SampleAggregator(_audioFile, 2048);
            _aggregator.FftCalculated += OnFftCalculated;
            _aggregator.PerformFFT = true;

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_aggregator);
        }

        private void OnFftCalculated(object sender, FftEventArgs e)
        {
            for (int i = 0; i < SpectrumData.Length; i++)
            {
                double magnitude = Math.Sqrt(
                    e.Result[i].X * e.Result[i].X +
                    e.Result[i].Y * e.Result[i].Y
                );
                SpectrumData[i] = SpectrumData[i] * 0.45f + (float)(magnitude * 0.003) * 0.55f;
            }
            FftDataAvailable?.Invoke(this, SpectrumData);
        }

        public void Play() => _waveOut?.Play();
        public void Pause() => _waveOut?.Pause();
        public void Stop()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFile?.Dispose();
            _waveOut = null;
            _audioFile = null;
        }
        public void SetVolume(float vol) { if (_audioFile != null) _audioFile.Volume = vol; }
        public void Seek(double seconds)
        {
            if (_audioFile != null)
                _audioFile.CurrentTime = TimeSpan.FromSeconds(seconds);
        }

        public void Dispose() => Stop();
    }
}