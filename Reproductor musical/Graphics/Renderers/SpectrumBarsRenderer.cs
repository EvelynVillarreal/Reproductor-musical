using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class SpectrumBarsRenderer
    {
        public void Render(Graphics g, int width, int height, float[] spectrum, float time)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int barCount = 80;
            float barWidth = (float)width / barCount;
            int centerY = height / 2;

            for (int i = 0; i < barCount; i++)
            {
                float specIndex = (float)i / barCount * 120;
                float magnitude = spectrum[(int)specIndex] * height * 1f;
                magnitude = Math.Max(magnitude, 1f);
                magnitude = Math.Min(magnitude, height / 2f);

                float hue = (float)i / barCount * 280f + time * 20f;
                hue %= 360f;
                Color barColor = VisualUtils.HsvToColor(hue, 1f, 1f);

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
    }
}
