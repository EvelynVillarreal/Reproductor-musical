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
                float specIndex = (float)i / barCount * 120;
                float targetMagnitude = spectrum[(int)specIndex] * height * 1.2f;
                targetMagnitude = Math.Min(targetMagnitude, height / 2f);

                if (targetMagnitude > _smoothedBars[i])
                    _smoothedBars[i] = targetMagnitude;
                else
                    _smoothedBars[i] = _smoothedBars[i] * 0.88f + targetMagnitude * 0.12f;

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
