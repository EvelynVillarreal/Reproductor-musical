using System;
using System.Drawing;

namespace Reproductor_musical.Visuals
{
    public class GeometricPulseRenderer
    {
        private readonly Pen _pen = new Pen(Color.White, 1.5f);

        public void Render(Graphics g, int width, int height, float bassEnergy, float midEnergy, float highEnergy, float time)
        {
            float cx = width / 2f, cy = height / 2f;
            float maxRadius = Math.Min(width, height) * 0.35f;

            for (int layer = 0; layer < 6; layer++)
            {
                float layerScale = 1f - layer * 0.12f;
                float radius = maxRadius * layerScale * (1f + bassEnergy * 0.3f);
                int points = 4 + layer * 2 + (int)(highEnergy * 8f);
                float rotation = time * (20f + midEnergy * 30f) * (layer % 2 == 0 ? 1 : -1) + layer * 30f;
                float hue = (layer * 50f + time * 40f + highEnergy * 100f) % 360f;
                int alpha = 80 + (int)(bassEnergy * 100f);

                PointF[] star = VisualUtils.StarPolygon(cx, cy, radius * 0.5f, radius, points, rotation);

                _pen.Color = Color.FromArgb(Math.Min(alpha, 255), VisualUtils.HsvToColor(hue, 1f, 1f));
                _pen.Width = 1.5f + bassEnergy * 3f;
                g.DrawPolygon(_pen, star);
            }
        }
    }
}
