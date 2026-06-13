using System;
using System.Drawing;

namespace Reproductor_musical.Visuals
{
    public class SpectrumBarsRenderer
    {
        private readonly float[] _smoothedBars = new float[80];
        private readonly SolidBrush _brush = new SolidBrush(Color.White);

        public void Render(Graphics g, int width, int height, float[] spectrum, float time)
        {
            int barCount = 80;
            float barWidth = (float)width / barCount;
            int centerY = height / 2;

            for (int i = 0; i < barCount; i++)
            {
                float n = (float)i / barCount;

                float floatingIndex = (float)Math.Pow(n, 1.15) * 119;
                int idxLow = (int)floatingIndex;
                int idxHigh = Math.Min(idxLow + 1, spectrum.Length - 1);
                float frac = floatingIndex - idxLow;
                float rawValue = spectrum[idxLow] * (1f - frac) + spectrum[idxHigh] * frac;

                float ganancia = 1.0f + n * 3.0f;
                float targetMagnitude = rawValue * height * ganancia;
                targetMagnitude = Math.Min(targetMagnitude, height / 2f);

                float diff = targetMagnitude - _smoothedBars[i];
                _smoothedBars[i] += diff * (diff > 0 ? 0.55f : 0.35f);

                float magnitude = Math.Max(_smoothedBars[i], 2f);

                float hue = (float)i / barCount * 280f + time * 20f;
                hue %= 360f;
                Color barColor = VisualUtils.HsvToColor(hue, 0.8f, 1f);

                float x = i * barWidth;

                _brush.Color = Color.FromArgb(140, barColor);
                g.FillRectangle(_brush, x + 1, centerY - magnitude, barWidth - 2, magnitude * 2);

                _brush.Color = Color.White;
                g.FillRectangle(_brush, x + 1, centerY - magnitude, barWidth - 2, 2);
                g.FillRectangle(_brush, x + 1, centerY + magnitude - 2, barWidth - 2, 2);
            }
        }
    }
}
