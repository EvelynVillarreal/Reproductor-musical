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
        private readonly float[] _smoothedWave = new float[CirclePoints];

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
            float pulseRadius = baseRadius + (bassEnergy * 350f) + (midEnergy * 120f);

            PointF[] outerPts = new PointF[CirclePoints];
            PointF[] midPts = new PointF[CirclePoints];
            PointF[] innerPts = new PointF[CirclePoints];

            float hueBase = (time * 30f) % 360f;

            for (int i = 0; i < CirclePoints; i++)
            {
                // Simetría
                int halfPoints = CirclePoints / 2;
                float percent = i <= halfPoints
                    ? (float)i / halfPoints
                    : (float)(CirclePoints - i) / halfPoints;

                // 2. CURVA DE POTENCIA LOGARÍTMICA
                float floatingIndex = (float)Math.Pow(percent, 1.15) * 119;
                int idxLow = (int)floatingIndex;
                int idxHigh = Math.Min(idxLow + 1, spectrum.Length - 1);
                float frac = floatingIndex - idxLow;
                float rawValue = spectrum[idxLow] * (1f - frac) + spectrum[idxHigh] * frac;

                // Ganancia dinámica para levantar los agudos
                float ganancia = 1.0f + percent * 3.0f;
                float targetValue = rawValue * ganancia;

                // 3. FÍSICA ASIMÉTRICA (Fluidez y Ataque/Decaimiento)
                float diff = targetValue - _smoothedWave[i];
                _smoothedWave[i] += diff * (diff > 0 ? 0.55f : 0.35f);
                float smoothVal = _smoothedWave[i];

                // 4. CÁLCULOS FINALES DE LA ONDA
                float wave = (float)Math.Pow(smoothVal, 1.2) * 400f;

                // Usamos índices desfasados para las ondas internas para dar profundidad 3D
                int midIdx = (i + 30) % CirclePoints;
                float midWave = _smoothedWave[midIdx] * 80f;

                int innerIdx = (i + 60) % CirclePoints;
                float highWave = _smoothedWave[innerIdx] * 40f;

                float r = pulseRadius + wave;
                float rMid = pulseRadius * 0.65f + midWave + bassEnergy * 30f;
                float rInner = pulseRadius * 0.3f + highWave + midEnergy * 20f;

                outerPts[i] = new PointF(cx + _cosTable[i] * r, cy + _sinTable[i] * r);
                midPts[i] = new PointF(cx + _cosTable[i] * rMid, cy + _sinTable[i] * rMid);
                innerPts[i] = new PointF(cx + _cosTable[i] * rInner, cy + _sinTable[i] * rInner);
            }

            // --- RENDERIZADO DE LOS POLÍGONOS (Sin cambios) ---
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

            // --- DESTELLOS RÍTMICOS (Spikes) ---
            int spikeCount = 36;
            _pen.Width = 1.5f;
            for (int i = 0; i < spikeCount; i++)
            {   
                int lutIdx = (i * (CirclePoints / spikeCount)) % CirclePoints;

                // Usamos la onda suavizada para los picos también para evitar saltos raros
                float smoothSpike = _smoothedWave[lutIdx];
                float spikeLen = smoothSpike * 350f + highEnergy * 150f;
                float innerLen = 5f + bassEnergy * 30f;

                PointF p1 = new PointF(cx + _cosTable[lutIdx] * innerLen, cy + _sinTable[lutIdx] * innerLen);
                PointF p2 = new PointF(cx + _cosTable[lutIdx] * (pulseRadius * 0.3f + spikeLen), cy + _sinTable[lutIdx] * (pulseRadius * 0.3f + spikeLen));

                _pen.Color = Color.FromArgb(100, VisualUtils.HsvToColor((hueBase + i * 10f) % 360f, 1f, 1f));
                g.DrawLine(_pen, p1, p2);
            }

            // Esfera central
            float centerSize = pulseRadius * 0.2f + bassEnergy * 40f + highEnergy * 20f;
            _brush.Color = Color.FromArgb(180, VisualUtils.HsvToColor((hueBase + 180) % 360, 1f, 1f));
            g.FillEllipse(_brush, cx - centerSize / 2f, cy - centerSize / 2f, centerSize, centerSize);
        }
    }
}