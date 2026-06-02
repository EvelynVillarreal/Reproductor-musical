using System;
using System.Drawing;
using System.Drawing.Drawing2D;

public enum VisualizationMode { SpectrumBars, Particles, WaveCircle, GeometricPulse }

public class Visualizer
{
    private readonly ParticleSystem _particles;
    private float[] _spectrum = new float[256];
    private float _bassEnergy, _midEnergy, _highEnergy;
    private float _time = 0f;

    public VisualizationMode Mode { get; set; } = VisualizationMode.SpectrumBars;
    public Color PrimaryColor { get; set; } = Color.Cyan;

    public Visualizer()
    {
        _particles = new ParticleSystem(500);
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

        DrawDynamicBackground(g, width, height);

        switch (Mode)
        {
            case VisualizationMode.SpectrumBars:
                DrawSpectrumBars(g, width, height);
                break;
            case VisualizationMode.Particles:
                DrawParticles(g, width, height);
                break;
            case VisualizationMode.WaveCircle:
                DrawCircularWave(g, width, height);
                break;
            case VisualizationMode.GeometricPulse:
                DrawGeometricPulse(g, width, height);
                break;
        }

        DrawWaveform(g, width, height);
    }

    private void DrawSpectrumBars(Graphics g, int width, int height)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int barCount = 80;
        float barWidth = (float)width / barCount;
        int centerY = height / 2;

        for (int i = 0; i < barCount; i++)
        {
            float specIndex = (float)i / barCount * 120;
            float magnitude = _spectrum[(int)specIndex] * height * 0.8f;
            magnitude = Math.Max(magnitude, 1f);
            magnitude = Math.Min(magnitude, height / 2f);

            float hue = (float)i / barCount * 280f + _time * 20f;
            hue %= 360f;
            Color barColor = HsvToColor(hue, 1f, 1f);

            float x = i * barWidth;
            using (var brush = new LinearGradientBrush(
                new PointF(x, centerY - magnitude),
                new PointF(x, centerY + magnitude),
                barColor, Color.FromArgb(50, barColor)))
            {
                g.FillRectangle(brush,
                    x + 1, centerY - magnitude,
                    barWidth - 2, magnitude * 2);
            }

            using (var glowBrush = new SolidBrush(Color.FromArgb(200, barColor)))
            {
                g.FillRectangle(glowBrush, x + 1, centerY - magnitude, barWidth - 2, 3);
            }
        }
    }

    private void DrawParticles(Graphics g, int width, int height)
    {
        _particles.Update(_bassEnergy, _midEnergy, width, height, _time);
        _particles.Draw(g, _time);
    }

    private void DrawCircularWave(Graphics g, int width, int height)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cx = width / 2f, cy = height / 2f;
        float baseRadius = Math.Min(width, height) * 0.25f;
        float pulseRadius = baseRadius + _bassEnergy * 80f;

        int points = 180;
        PointF[] outerPts = new PointF[points];
        PointF[] innerPts = new PointF[points];

        for (int i = 0; i < points; i++)
        {
            float angle = (float)i / points * (float)(Math.PI * 2);
            int specIdx = (int)((float)i / points * 100);
            float wave = _spectrum[specIdx] * 120f;

            float r = pulseRadius + wave;
            outerPts[i] = new PointF(cx + (float)Math.Cos(angle) * r,
                                      cy + (float)Math.Sin(angle) * r);
            innerPts[i] = new PointF(cx + (float)Math.Cos(angle) * pulseRadius,
                                      cy + (float)Math.Sin(angle) * pulseRadius);
        }

        using (var path = new GraphicsPath())
        {
            path.AddPolygon(outerPts);
            float hue = (_time * 40f) % 360f;
            using (var brush = new SolidBrush(Color.FromArgb(120, HsvToColor(hue, 1f, 1f))))
            {
                g.FillPath(brush, path);
            }

            using (var pen = new Pen(HsvToColor(hue, 0.8f, 1f), 2f))
            {
                g.DrawPolygon(pen, outerPts);
            }

            using (var centerBrush = new SolidBrush(Color.FromArgb(150, HsvToColor((hue + 180) % 360, 1f, 1f))))
            {
                g.FillEllipse(centerBrush,
                    cx - pulseRadius * 0.3f, cy - pulseRadius * 0.3f,
                    pulseRadius * 0.6f, pulseRadius * 0.6f);
            }
        }
    }

    private void DrawGeometricPulse(Graphics g, int width, int height)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cx = width / 2f, cy = height / 2f;

        for (int layer = 5; layer >= 1; layer--)
        {
            float scale = layer * 0.2f + _bassEnergy * 0.5f;
            float rotation = _time * (layer % 2 == 0 ? 30f : -20f) + layer * 15f;
            int sides = 3 + layer;
            float radius = Math.Min(width, height) * 0.1f * scale * (1 + _midEnergy);
            float alpha = 60 + layer * 30;
            float hue = (layer * 60f + _time * 50f) % 360f;

            PointF[] poly = RegularPolygon(cx, cy, radius, sides, rotation);
            using (var pen = new Pen(Color.FromArgb((int)alpha, HsvToColor(hue, 1f, 1f)), 2f))
            {
                g.DrawPolygon(pen, poly);
            }
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

    private void DrawDynamicBackground(Graphics g, int w, int h)
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

    private PointF[] RegularPolygon(float cx, float cy, float r, int sides, float rotDeg)
    {
        var pts = new PointF[sides];
        float rot = rotDeg * (float)Math.PI / 180f;
        for (int i = 0; i < sides; i++)
        {
            float a = rot + (float)i / sides * (float)Math.PI * 2;
            pts[i] = new PointF(cx + (float)Math.Cos(a) * r, cy + (float)Math.Sin(a) * r);
        }
        return pts;
    }

    public static Color HsvToColor(float h, float s, float v)
    {
        h = h % 360f;
        float c = v * s;
        float x = c * (1 - Math.Abs(h / 60f % 2 - 1));
        float m = v - c;
        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        return Color.FromArgb(
            (int)((r + m) * 255),
            (int)((g + m) * 255),
            (int)((b + m) * 255));
    }
}
