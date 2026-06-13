using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class GeometricPulseRenderer
    {
        private readonly Pen _pen = new Pen(Color.White, 1.5f);
        private readonly SolidBrush _brush = new SolidBrush(Color.White);

        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float midEnergy, float highEnergy, float time, float smoothedBass, float smoothedMid, float smoothedHigh)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = width / 2f, cy = height / 2f;
            float maxRadius = Math.Min(width, height) * 0.35f;

            for (int layer = 0; layer < 6; layer++)
            {
                float layerScale = 1f - layer * 0.12f;
                float radius = maxRadius * layerScale * (1f + bassEnergy * 1.5f);
                int points = 4 + layer * 2 + (int)(smoothedHigh * 8f);
                float rotation = time * (20f + smoothedMid * 30f) * (layer % 2 == 0 ? 1 : -1) + layer * 30f;
                float hue = (layer * 50f + time * 40f + smoothedHigh * 100f) % 360f;
                int alpha = 100 + (int)(smoothedBass * 100f);

                PointF[] star = VisualUtils.StarPolygon(cx, cy, radius * 0.5f, radius, points, rotation);

                _pen.Color = Color.FromArgb(Math.Min(alpha, 255), VisualUtils.HsvToColor(hue, 1f, 1f));
                _pen.Width = 3f + smoothedBass * 4f;
                g.DrawPolygon(_pen, star);

                if (layer > 0 && layer % 2 == 0)
                {
                    int totalVertices = points * 2;
                    for (int i = 0; i < totalVertices; i++)
                    {
                        int next = (i + 2) % totalVertices;
                        float lineHue = (hue + i * 10f) % 360f;
                        _pen.Color = Color.FromArgb(120, VisualUtils.HsvToColor(lineHue, 0.8f, 1f));
                        _pen.Width = 2f + smoothedMid * 1.5f;
                        g.DrawLine(_pen, star[i], star[next]);
                    }
                }
            }

            float centerSize = maxRadius * 0.15f * (1f + bassEnergy * 0.8f);
            float centerHue = (time * 60f + smoothedHigh * 200f) % 360f;
            _brush.Color = Color.FromArgb(200, VisualUtils.HsvToColor(centerHue, 1f, 1f));
            g.FillEllipse(_brush, cx - centerSize / 2f, cy - centerSize / 2f, centerSize, centerSize);

            int orbitCount = 8 + (int)(smoothedMid * 12f);
            PointF[] orbitPoints = new PointF[orbitCount];
            for (int i = 0; i < orbitCount; i++)
            {
                float angle = (float)i / orbitCount * (float)(Math.PI * 2) + time * (1f + smoothedMid);
                float dist = maxRadius * 0.6f * (1f + spectrum[(i * 10) % spectrum.Length] * 0.5f) * (1f + bassEnergy * 0.3f);
                float dotSize = 4f + smoothedHigh * 10f;
                float dotHue = (centerHue + i * (360f / orbitCount)) % 360f;

                orbitPoints[i] = new PointF(cx + (float)Math.Cos(angle) * dist, cy + (float)Math.Sin(angle) * dist);

                _brush.Color = VisualUtils.HsvToColor(dotHue, 1f, 1f);
                g.FillEllipse(_brush, orbitPoints[i].X - dotSize / 2, orbitPoints[i].Y - dotSize / 2, dotSize, dotSize);
            }

            if (orbitCount > 2)
            {
                _pen.Color = Color.FromArgb(90, Color.White);
                _pen.Width = 1.5f;
                g.DrawPolygon(_pen, orbitPoints);
            }
        }
    }
}
