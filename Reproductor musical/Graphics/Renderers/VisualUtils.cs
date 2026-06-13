using System;
using System.Drawing;

namespace Reproductor_musical.Visuals
{
    public static class VisualUtils
    {
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

        public static PointF[] RegularPolygon(float cx, float cy, float r, int sides, float rotDeg)
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

        public static PointF[] StarPolygon(float cx, float cy, float innerR, float outerR, int points, float rotDeg)
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
    }
}
