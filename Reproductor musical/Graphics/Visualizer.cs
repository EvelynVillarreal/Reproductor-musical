using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public enum VisualizationMode { SpectrumBars, Particles, WaveCircle, GeometricPulse }

    public class Visualizer
    {
        private readonly ParticleSystem _particles;
        private readonly SpectrumBarsRenderer _spectrumBars;
        private readonly WaveCircleRenderer _waveCircle;
        private readonly GeometricPulseRenderer _geometricPulse;

        private float[] _spectrum = new float[256];
        private float _bassEnergy, _midEnergy, _highEnergy;
        private float _time;

        public VisualizationMode Mode { get; set; } = VisualizationMode.SpectrumBars;

        public Visualizer()
        {
            _particles = new ParticleSystem(500);
            _spectrumBars = new SpectrumBarsRenderer();
            _waveCircle = new WaveCircleRenderer();
            _geometricPulse = new GeometricPulseRenderer();
        }

        public void UpdateSpectrum(float[] spectrum)
        {
            _spectrum = spectrum;
            _bassEnergy = Average(spectrum, 0, 10);
            _midEnergy = Average(spectrum, 10, 80);
            _highEnergy = Average(spectrum, 80, 200);
        }

        private float Average(float[] data, int start, int end)
        {
            float sum = 0;
            for (int i = start; i < end && i < data.Length; i++)
                sum += data[i];
            return sum / (end - start);
        }

        public void Render(Graphics g, int width, int height)
        {
            _time += 0.016f;
            DrawBackground(g, width, height);

            switch (Mode)
            {
                case VisualizationMode.SpectrumBars:
                    _spectrumBars.Render(g, width, height, _spectrum, _time);
                    break;
                case VisualizationMode.Particles:
                    _particles.Update(_bassEnergy, _midEnergy, width, height, _time);
                    _particles.Draw(g, _time);
                    break;
                case VisualizationMode.WaveCircle:
                    _waveCircle.Render(g, width, height, _spectrum, _bassEnergy, _midEnergy, _highEnergy, _time);
                    break;
                case VisualizationMode.GeometricPulse:
                    _geometricPulse.Render(g, width, height, _bassEnergy, _midEnergy, _highEnergy, _time);
                    break;
            }

            DrawWaveform(g, width, height);
        }

        private void DrawBackground(Graphics g, int w, int h)
        {
            float bass = Math.Min(_bassEnergy * 0.3f, 1f);
            int r = (int)(5 + bass * 30);
            int gVal = (int)(0 + bass * 10);
            int b = (int)(15 + bass * 40);

            using (var bg = new LinearGradientBrush(new Point(0, 0), new Point(w, h),
                Color.FromArgb(255, r, gVal, b),
                Color.FromArgb(255, r / 2, gVal / 2, b * 2)))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }
        }

        private void DrawWaveform(Graphics g, int width, int height)
        {
            int waveY = height - 60;
            float waveHeight = 40f + _bassEnergy * 20f;

            using (var pen = new Pen(Color.FromArgb(180, Color.White), 1.5f))
            {
                for (int i = 1; i < _spectrum.Length && i < width; i++)
                {
                    float x1 = (float)(i - 1) / _spectrum.Length * width;
                    float x2 = (float)i / _spectrum.Length * width;
                    float y1 = waveY - _spectrum[i - 1] * waveHeight;
                    float y2 = waveY - _spectrum[i] * waveHeight;
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
        }
    }
}
