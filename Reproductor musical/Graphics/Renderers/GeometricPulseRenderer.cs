using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class GeometricPulseRenderer
    {
        private readonly Pen _pen = new Pen(Color.White, 1.5f);
        private readonly SolidBrush _brush = new SolidBrush(Color.White);

        private float _rotacionAcumulada = 0f;

        private const int OrbitNodes = 24;
        private readonly float[] _smoothedOrbit = new float[OrbitNodes];

        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float midEnergy, float highEnergy, float time, float smoothedBass, float smoothedMid, float smoothedHigh)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = width / 2f, cy = height / 2f;

            // USAMOS ENERGÍA CRUDA para un impacto violento y sin inercia
            float bassImpact = (float)Math.Pow(bassEnergy, 2);

            // Aceleración mucho más agresiva en los golpes
            float velocidadGiro = 0.05f + (bassImpact * 45f);
            _rotacionAcumulada += velocidadGiro;

            float maxRadius = Math.Min(width, height) * 0.35f + (bassEnergy * 200f);

            // Polígonos de fondo
            int nSides1 = 3 + (int)(smoothedMid * 8f);
            int nSides2 = 5 + (int)(smoothedHigh * 6f);

            nSides1 = Math.Min(Math.Max(nSides1, 3), 11);
            nSides2 = Math.Min(Math.Max(nSides2, 3), 11);

            DrawRegularPolygon(g, cx, cy, maxRadius * 1.3f, nSides1, _rotacionAcumulada * 0.05f, bassEnergy, smoothedMid, time);
            DrawRegularPolygon(g, cx, cy, maxRadius * 0.8f, nSides2, -_rotacionAcumulada * 0.08f, bassEnergy, smoothedHigh, time + 10f);

            // --- RENDERIZADO DEL VÓRTICE CENTRAL (Estrellas) ---
            for (int layer = 0; layer < 6; layer++)
            {
                float layerScale = 1f - layer * (0.12f + bassImpact * 0.2f);
                float radius = maxRadius * layerScale;

                // Latido con energía cruda = saltos bruscos
                float latidoTamaño = 1f + (bassEnergy * 1.2f);
                radius *= latidoTamaño;

                if (radius < 5) continue;

                int basePoints = 4 + layer;
                int points = basePoints + (int)(smoothedHigh * 25f);

                float multiplicadorVelocidad = 1f - (layer * 0.08f);
                float rotationDir = layer % 2 == 0 ? 1 : -1;

                float rotation = (_rotacionAcumulada * multiplicadorVelocidad * rotationDir) + (layer * 45f);

                float hue = (layer * 40f + time * 50f + smoothedHigh * 150f) % 360f;
                int alpha = Math.Min(255, 60 + (int)(bassEnergy * 180f) + (layer == 0 ? 80 : 0));

                PointF[] star = VisualUtils.StarPolygon(cx, cy, radius * 0.4f, radius, points, rotation);

                _pen.Color = Color.FromArgb(alpha, VisualUtils.HsvToColor(hue, 1f, 1f));
                _pen.Width = 2f + (bassEnergy * 40f); // Grosor reacciona al instante
                g.DrawPolygon(_pen, star);

                if (layer > 0 && layer % 2 == 0)
                {
                    int totalVertices = points * 2;
                    for (int i = 0; i < totalVertices; i += 2)
                    {
                        int next = (i + 3) % totalVertices;
                        _pen.Color = Color.FromArgb(Math.Min(alpha, 80), VisualUtils.HsvToColor(hue, 0.8f, 1f));
                        _pen.Width = 1f + (midEnergy * 20f); // Medios en crudo
                        g.DrawLine(_pen, star[i], star[next]);
                    }
                }
            }

            // --- NÚCLEO CENTRAL ---
            float centerSize = maxRadius * 0.10f + (bassImpact * 100f);
            float centerHue = (time * 80f + smoothedHigh * 300f) % 360f;
            _brush.Color = Color.FromArgb(200, VisualUtils.HsvToColor(centerHue, 1f, 1f));
            g.FillEllipse(_brush, cx - centerSize / 2f, cy - centerSize / 2f, centerSize, centerSize);

            // ============================================================
            // --- RED DE ÓRBITA CAÓTICA ---
            // ============================================================
            PointF[] orbitPoints = new PointF[OrbitNodes];

            float baseOrbitRadius = maxRadius * 1.1f + (bassEnergy * 250f);
            float latidoOrbitaViolento = 1f + bassImpact * 3.0f;
            baseOrbitRadius *= latidoOrbitaViolento;

            for (int i = 0; i < OrbitNodes; i++)
            {
                float angle = (float)i / OrbitNodes * (float)(Math.PI * 2) + (_rotacionAcumulada * 0.01f);
                int specIdx = (i * 5) % spectrum.Length;

                float targetDist = baseOrbitRadius * (1f + spectrum[specIdx] * 2.0f);

                // INERCIA ASIMÉTRICA: Ataque del 85% (casi instantáneo), caída del 30%
                float diff = targetDist - _smoothedOrbit[i];
                _smoothedOrbit[i] += diff * (diff > 0 ? 0.85f : 0.30f);

                float localDist = _smoothedOrbit[i];

                orbitPoints[i] = new PointF(cx + (float)Math.Cos(angle) * localDist, cy + (float)Math.Sin(angle) * localDist);

                float dotSize = 3f + spectrum[specIdx] * 20f;
                _brush.Color = VisualUtils.HsvToColor((centerHue + i * 20f) % 360f, 1f, 1f);
                g.FillEllipse(_brush, orbitPoints[i].X - dotSize / 2, orbitPoints[i].Y - dotSize / 2, dotSize, dotSize);

                if (smoothedMid > 0.3f && i % 2 == 0)
                {
                    _pen.Color = Color.FromArgb(Math.Min(255, (int)(smoothedMid * 100f)), _brush.Color);
                    _pen.Width = 1f + (highEnergy * 25f);
                    g.DrawLine(_pen, orbitPoints[i].X, orbitPoints[i].Y, cx, cy);
                }
            }

            _pen.Color = Color.FromArgb(40 + (int)(highEnergy * 100f), Color.White);
            _pen.Width = 1f + (bassEnergy * 25f);
            g.DrawPolygon(_pen, orbitPoints);
        }

        private void DrawRegularPolygon(Graphics g, float cx, float cy, float radius, int sides, float rotation, float bass, float energy, float time)
        {
            if (radius < 5) return;
            PointF[] pts = new PointF[sides];

            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * (float)(Math.PI * 2) + rotation;
                pts[i] = new PointF(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius);
            }

            float hue = (time * 60f + energy * 200f) % 360f;
            _pen.Color = Color.FromArgb((int)(40 + bass * 150f), VisualUtils.HsvToColor(hue, 0.9f, 1f));
            _pen.Width = 2f + (bass * 30f);
            g.DrawPolygon(_pen, pts);
        }
    }
}