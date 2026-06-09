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
            float magnitude = _spectrum[(int)specIndex] * height * 1f;
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
        _particles.Update(_bassEnergy, _midEnergy, _highEnergy, width, height, _time);
        _particles.Draw(g, _time);
    }

    private void DrawCircularWave(Graphics g, int width, int height)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cx = width / 2f, cy = height / 2f;
        float baseRadius = Math.Min(width, height) * 0.2f;
        float pulseRadius = baseRadius + _bassEnergy * 120f;

        int points = 256;
        PointF[] outerPts = new PointF[points];
        PointF[] midPts = new PointF[points];
        PointF[] innerPts = new PointF[points];

        float hueBase = (_time * 30f) % 360f;

        for (int i = 0; i < points; i++)
        {
            float angle = (float)i / points * (float)(Math.PI * 2);
            float wave = _spectrum[i] * 180f;
            float midWave = _spectrum[(i + 50) % _spectrum.Length] * 80f;
            float highWave = _spectrum[(i + 100) % _spectrum.Length] * 40f;

            float r = pulseRadius + wave;
            float rMid = pulseRadius * 0.65f + midWave + _bassEnergy * 30f;
            float rInner = pulseRadius * 0.3f + highWave + _midEnergy * 20f;

            outerPts[i] = new PointF(cx + (float)Math.Cos(angle) * r,
                                      cy + (float)Math.Sin(angle) * r);
            midPts[i] = new PointF(cx + (float)Math.Cos(angle) * rMid,
                                    cy + (float)Math.Sin(angle) * rMid);
            innerPts[i] = new PointF(cx + (float)Math.Cos(angle) * rInner,
                                      cy + (float)Math.Sin(angle) * rInner);
        }

        using (var path = new GraphicsPath())
        {
            path.AddPolygon(outerPts);
            using (var brush = new SolidBrush(Color.FromArgb(60, HsvToColor(hueBase, 0.8f, 1f))))
                g.FillPath(brush, path);
            using (var pen = new Pen(HsvToColor(hueBase, 0.9f, 1f), 2f))
                g.DrawPolygon(pen, outerPts);
        }

        using (var path = new GraphicsPath())
        {
            path.AddPolygon(midPts);
            using (var brush = new SolidBrush(Color.FromArgb(80, HsvToColor((hueBase + 120) % 360, 0.8f, 1f))))
                g.FillPath(brush, path);
            using (var pen = new Pen(HsvToColor((hueBase + 120) % 360, 0.9f, 1f), 1.5f))
                g.DrawPolygon(pen, midPts);
        }

        using (var path = new GraphicsPath())
        {
            path.AddPolygon(innerPts);
            using (var brush = new SolidBrush(Color.FromArgb(100, HsvToColor((hueBase + 240) % 360, 0.8f, 1f))))
                g.FillPath(brush, path);
            using (var pen = new Pen(HsvToColor((hueBase + 240) % 360, 0.9f, 1f), 1.5f))
                g.DrawPolygon(pen, innerPts);
        }

        int spikeCount = 36;
        for (int i = 0; i < spikeCount; i++)
        {
            float angle = (float)i / spikeCount * (float)(Math.PI * 2) + _time * 0.5f;
            int specIdx = (i * 7) % _spectrum.Length;
            float spikeLen = _spectrum[specIdx] * 150f + _highEnergy * 50f;
            float innerLen = 5f + _bassEnergy * 30f;

            PointF p1 = new PointF(cx + (float)Math.Cos(angle) * innerLen,
                                   cy + (float)Math.Sin(angle) * innerLen);
            PointF p2 = new PointF(cx + (float)Math.Cos(angle) * (pulseRadius * 0.3f + spikeLen),
                                   cy + (float)Math.Sin(angle) * (pulseRadius * 0.3f + spikeLen));
            float hue = (hueBase + i * 10f) % 360f;
            using (var pen = new Pen(Color.FromArgb(100, HsvToColor(hue, 1f, 1f)), 1.5f))
                g.DrawLine(pen, p1, p2);
        }

        float centerSize = pulseRadius * 0.2f + _bassEnergy * 40f + _highEnergy * 20f;
        using (var centerBrush = new SolidBrush(Color.FromArgb(180, HsvToColor((hueBase + 180) % 360, 1f, 1f))))
        {
            g.FillEllipse(centerBrush,
                cx - centerSize / 2f, cy - centerSize / 2f,
                centerSize, centerSize);
        }

        int dotCount = 12;
        for (int i = 0; i < dotCount; i++)
        {
            float angle = (float)i / dotCount * (float)(Math.PI * 2) + _time * (1f + _midEnergy * 2f);
            float orbitRadius = pulseRadius * 0.7f + _spectrum[(i * 20) % _spectrum.Length] * 50f;
            float dotSize = 3f + _spectrum[(i * 15) % _spectrum.Length] * 15f;
            PointF dotPos = new PointF(
                cx + (float)Math.Cos(angle) * orbitRadius,
                cy + (float)Math.Sin(angle) * orbitRadius);
            float hue = (hueBase + i * 30f) % 360f;
            using (var dotBrush = new SolidBrush(HsvToColor(hue, 1f, 1f)))
                g.FillEllipse(dotBrush, dotPos.X - dotSize / 2, dotPos.Y - dotSize / 2, dotSize, dotSize);
        }
    }

    private void DrawGeometricPulse(Graphics g, int width, int height)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cx = width / 2f, cy = height / 2f;
        float maxRadius = Math.Min(width, height) * 0.35f;

        for (int layer = 0; layer < 6; layer++)
        {
            float layerScale = 1f - layer * 0.12f;
            float radius = maxRadius * layerScale * (1f + _bassEnergy * 0.3f);
            int points = 4 + layer * 2 + (int)(_highEnergy * 8f);
            float rotation = _time * (20f + _midEnergy * 30f) * (layer % 2 == 0 ? 1 : -1) + layer * 30f;
            float hue = (layer * 50f + _time * 40f + _highEnergy * 100f) % 360f;
            int alpha = 80 + (int)(_bassEnergy * 100f);

            PointF[] star = StarPolygon(cx, cy, radius * 0.5f, radius, points, rotation);

            using (var pen = new Pen(Color.FromArgb(Math.Min(alpha, 255), HsvToColor(hue, 1f, 1f)), 1.5f + _bassEnergy * 3f))
            {
                g.DrawPolygon(pen, star);
            }

            if (layer > 0 && layer % 2 == 0)
            {
                for (int i = 0; i < points; i++)
                {
                    int next = (i + 2) % points;
                    float hueLine = (hue + i * 20f) % 360f;
                    using (var linePen = new Pen(Color.FromArgb(40, HsvToColor(hueLine, 0.8f, 1f)), 1f))
                    {
                        g.DrawLine(linePen, star[i], star[next]);
                    }
                }
            }
        }

        float centerSize = maxRadius * 0.15f * (1f + _bassEnergy * 0.8f);
        float centerHue = (_time * 60f + _highEnergy * 200f) % 360f;
        using (var centerBrush = new SolidBrush(Color.FromArgb(200, HsvToColor(centerHue, 1f, 1f))))
        {
            g.FillEllipse(centerBrush,
                cx - centerSize / 2f, cy - centerSize / 2f,
                centerSize, centerSize);
        }

        int orbitCount = 8 + (int)(_midEnergy * 12f);
        PointF[] orbitPts = new PointF[orbitCount];
        for (int i = 0; i < orbitCount; i++)
        {
            float angle = (float)i / orbitCount * (float)(Math.PI * 2) + _time * (1f + _midEnergy);
            float orbitDist = maxRadius * 0.6f * (1f + _spectrum[(i * 10) % _spectrum.Length] * 0.5f);
            float size = 2f + _spectrum[(i * 8) % _spectrum.Length] * 20f;
            float hue = (centerHue + i * (360f / orbitCount)) % 360f;

            orbitPts[i] = new PointF(
                cx + (float)Math.Cos(angle) * orbitDist,
                cy + (float)Math.Sin(angle) * orbitDist);

            using (var brush = new SolidBrush(HsvToColor(hue, 1f, 1f)))
            {
                g.FillEllipse(brush, orbitPts[i].X - size / 2, orbitPts[i].Y - size / 2, size, size);
            }
        }

        if (orbitCount > 2)
        {
            using (var webPen = new Pen(Color.FromArgb(30, Color.White), 0.5f))
            {
                g.DrawPolygon(webPen, orbitPts);
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

    private PointF[] StarPolygon(float cx, float cy, float innerR, float outerR, int points, float rotDeg)
    {
        var pts = new PointF[points * 2];
        float rot = rotDeg * (float)Math.PI / 180f;
        for (int i = 0; i < points * 2; i++)
        {
            float a = rot + (float)i / (points * 2) * (float)Math.PI * 2;
            float r = (i % 2 == 0) ? outerR : innerR;
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