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

        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float midEnergy, float highEnergy, float time, float smoothedBass, float smoothedMid, float smoothedHigh)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = width / 2f, cy = height / 2f;

            // Velocidad base del vórtice central
            float velocidadGiro = 0.5f + (smoothedBass * 6f);
            _rotacionAcumulada += velocidadGiro;

            float maxRadius = Math.Min(width, height) * 0.35f + (smoothedBass * 180f);
            float bassImpact = (float)Math.Pow(smoothedBass, 2);

            // --- RENDERIZADO DEL VÓRTICE CENTRAL (Estrellas) ---
            for (int layer = 0; layer < 6; layer++)
            {
                float layerScale = 1f - layer * (0.12f + bassImpact * 0.15f);
                float radius = maxRadius * layerScale;

                // TAMAÑO PULSANTE (Latido suave)
                float latidoTamaño = 1f + (smoothedBass * 0.8f);
                radius *= latidoTamaño;

                if (radius < 5) continue;

                int basePoints = 4 + layer;
                int points = basePoints + (int)(smoothedHigh * 25f);

                // ROTACIÓN DIFERENCIAL (Paralaje)
                float multiplicadorVelocidad = 1f - (layer * 0.08f);
                float rotationDir = layer % 2 == 0 ? 1 : -1;

                float rotation = (_rotacionAcumulada * multiplicadorVelocidad * rotationDir) + (layer * 45f);

                float hue = (layer * 40f + time * 50f + smoothedHigh * 150f) % 360f;
                int alpha = Math.Min(255, 60 + (int)(smoothedBass * 150f) + (layer == 0 ? 80 : 0));

                PointF[] star = VisualUtils.StarPolygon(cx, cy, radius * 0.4f, radius, points, rotation);

                _pen.Color = Color.FromArgb(alpha, VisualUtils.HsvToColor(hue, 1f, 1f));
                _pen.Width = 2f + (smoothedBass * 35f);
                g.DrawPolygon(_pen, star);

                if (layer > 0 && layer % 2 == 0)
                {
                    int totalVertices = points * 2;
                    for (int i = 0; i < totalVertices; i += 2)
                    {
                        int next = (i + 3) % totalVertices;
                        _pen.Color = Color.FromArgb(Math.Min(alpha, 80), VisualUtils.HsvToColor(hue, 0.8f, 1f));
                        _pen.Width = 1f + (smoothedMid * 15f);
                        g.DrawLine(_pen, star[i], star[next]);
                    }
                }
            }

            // --- NÚCLEO CENTRAL ---
            float centerSize = maxRadius * 0.10f + (bassImpact * 80f);
            float centerHue = (time * 80f + smoothedHigh * 300f) % 360f;
            _brush.Color = Color.FromArgb(200, VisualUtils.HsvToColor(centerHue, 1f, 1f));
            g.FillEllipse(_brush, cx - centerSize / 2f, cy - centerSize / 2f, centerSize, centerSize);

            // ============================================================
            // --- RED DE ÓRBITA CAÓTICA (La Telaraña) - ACTUALIZADA ---
            // ============================================================
            int orbitCount = 12 + (int)(smoothedMid * 20f);
            PointF[] orbitPoints = new PointF[orbitCount];

            // 1. TAMAÑO VIOLENTO (Cálculo no lineal)
            // Aumentamos drásticamente el empuje base de los graves (de 50f a 200f)
            float orbitRadius = maxRadius * 1.1f + (smoothedBass * 200f);

            // Aplicamos un latido EXPONENCIAL mucho más agresivo.
            // Si el bajo está alto (0.8), esto añade un multiplicador masivo instantáneo.
            float latidoOrbitaViolento = 1f + (float)Math.Pow(smoothedBass, 2) * 2.0f;
            orbitRadius *= latidoOrbitaViolento;

            for (int i = 0; i < orbitCount; i++)
            {
                // 2. ROTACIÓN LENTA
                // Reducimos el multiplicador drásticamente de 0.06f a solo 0.01f.
                float angle = (float)i / orbitCount * (float)(Math.PI * 2) + (_rotacionAcumulada * 0.01f);

                int specIdx = (i * 5) % spectrum.Length;

                // 3. SALTOS VIOLENTOS INDIVIDUALES
                // Exageramos la reacción al espectro local (de 0.4f a 1.5f).
                // Esto hará que las bolitas individuales "exploten" hacia afuera rítmicamente.
                float localDist = orbitRadius * (1f + spectrum[specIdx] * 1.5f);

                orbitPoints[i] = new PointF(cx + (float)Math.Cos(angle) * localDist, cy + (float)Math.Sin(angle) * localDist);

                // Tamaño del punto reactivo
                float dotSize = 3f + spectrum[specIdx] * 15f;
                _brush.Color = VisualUtils.HsvToColor((centerHue + i * 20f) % 360f, 1f, 1f);
                g.FillEllipse(_brush, orbitPoints[i].X - dotSize / 2, orbitPoints[i].Y - dotSize / 2, dotSize, dotSize);

                // Telaraña láser (reacciona a los medios)
                if (smoothedMid > 0.3f && i % 2 == 0)
                {
                    _pen.Color = Color.FromArgb(Math.Min(255, (int)(smoothedMid * 100f)), _brush.Color);
                    _pen.Width = 1f + (smoothedHigh * 20f);
                    g.DrawLine(_pen, orbitPoints[i].X, orbitPoints[i].Y, cx, cy);
                }
            }

            // Dibujado del polígono de órbita que palpita con el bajo
            if (orbitCount > 2)
            {
                _pen.Color = Color.FromArgb(40 + (int)(smoothedHigh * 80f), Color.White);
                _pen.Width = 1f + (smoothedBass * 15f);
                g.DrawPolygon(_pen, orbitPoints);
            }
        }
    }
}