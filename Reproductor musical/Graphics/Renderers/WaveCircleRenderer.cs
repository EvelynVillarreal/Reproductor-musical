using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class WaveCircleRenderer
    {
        private const int CirclePoints = 256;
        private readonly float[] _cosTable = new float[CirclePoints];
        private readonly float[] _sinTable = new float[CirclePoints];
        private readonly SolidBrush _brush = new SolidBrush(Color.White);
        private readonly Pen _pen = new Pen(Color.White, 1.5f);

        public WaveCircleRenderer()
        {
            for (int i = 0; i < CirclePoints; i++)
            {
                float angle = (float)i / CirclePoints * (float)(Math.PI * 2);
                _cosTable[i] = (float)Math.Cos(angle);
                _sinTable[i] = (float)Math.Sin(angle);
            }
        }

        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float midEnergy, float highEnergy, float time)
        {
            float cx = width / 2f, cy = height / 2f;
            float baseRadius = Math.Min(width, height) * 0.2f;
            float pulseRadius = baseRadius + bassEnergy * 120f;

            PointF[] outerPts = new PointF[CirclePoints];
            PointF[] midPts = new PointF[CirclePoints];
            PointF[] innerPts = new PointF[CirclePoints];

            float hueBase = (time * 30f) % 360f;

            for (int i = 0; i < CirclePoints; i++)
            {
                float wave = spectrum[i] * 180f;
                float midWave = spectrum[(i + 50) % spectrum.Length] * 80f;
                float highWave = spectrum[(i + 100) % spectrum.Length] * 40f;

                float r = pulseRadius + wave;
                float rMid = pulseRadius * 0.65f + midWave + bassEnergy * 30f;
                float rInner = pulseRadius * 0.3f + highWave + midEnergy * 20f;

                outerPts[i] = new PointF(cx + _cosTable[i] * r, cy + _sinTable[i] * r);
                midPts[i] = new PointF(cx + _cosTable[i] * rMid, cy + _sinTable[i] * rMid);
                innerPts[i] = new PointF(cx + _cosTable[i] * rInner, cy + _sinTable[i] * rInner);
            }

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(outerPts);
                _brush.Color = Color.FromArgb(40, VisualUtils.HsvToColor(hueBase, 0.8f, 1f));
                g.FillPath(_brush, path);
                _pen.Color = VisualUtils.HsvToColor(hueBase, 0.9f, 1f);
                _pen.Width = 2f;
                g.DrawPolygon(_pen, outerPts);
            }

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(midPts);
                _brush.Color = Color.FromArgb(50, VisualUtils.HsvToColor((hueBase + 120) % 360, 0.8f, 1f));
                g.FillPath(_brush, path);
                _pen.Color = VisualUtils.HsvToColor((hueBase + 120) % 360, 0.9f, 1f);
                _pen.Width = 1.5f;
                g.DrawPolygon(_pen, midPts);
            }

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(innerPts);
                _brush.Color = Color.FromArgb(60, VisualUtils.HsvToColor((hueBase + 240) % 360, 0.8f, 1f));
                g.FillPath(_brush, path);
                _pen.Color = VisualUtils.HsvToColor((hueBase + 240) % 360, 0.9f, 1f);
                _pen.Width = 1.5f;
                g.DrawPolygon(_pen, innerPts);
            }

            int spikeCount = 36;
            _pen.Width = 1.5f;
            for (int i = 0; i < spikeCount; i++)
            {
                int lutIdx = (i * (CirclePoints / spikeCount)) % CirclePoints;
                int specIdx = (i * 7) % spectrum.Length;
                float spikeLen = spectrum[specIdx] * 150f + highEnergy * 50f;
                float innerLen = 5f + bassEnergy * 30f;

                PointF p1 = new PointF(cx + _cosTable[lutIdx] * innerLen, cy + _sinTable[lutIdx] * innerLen);
                PointF p2 = new PointF(cx + _cosTable[lutIdx] * (pulseRadius * 0.3f + spikeLen), cy + _sinTable[lutIdx] * (pulseRadius * 0.3f + spikeLen));

                _pen.Color = Color.FromArgb(100, VisualUtils.HsvToColor((hueBase + i * 10f) % 360f, 1f, 1f));
                g.DrawLine(_pen, p1, p2);
            }

            float centerSize = pulseRadius * 0.2f + bassEnergy * 40f + highEnergy * 20f;
            _brush.Color = Color.FromArgb(180, VisualUtils.HsvToColor((hueBase + 180) % 360, 1f, 1f));
            g.FillEllipse(_brush, cx - centerSize / 2f, cy - centerSize / 2f, centerSize, centerSize);
        }
    }
}
