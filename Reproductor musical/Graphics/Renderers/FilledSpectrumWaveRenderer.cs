using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class FilledSpectrumWaveRenderer
    {
        private const int PointCount = 100;
        private readonly float[] _smoothedWavePoints = new float[PointCount];
        private readonly Pen _outlinePen = new Pen(Color.White, 2.5f);

        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float highEnergy, float time)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int groundY = (int)(height * 0.7f);
            float spectrumPower = 1.0f + bassEnergy * 1.5f;

            PointF[] topCurve = new PointF[PointCount];

            for (int i = 0; i < PointCount; i++)
            {

                // Calculamos 'n' de 0.0 a 1.0 a lo largo del ancho
                float n = (float)i / (PointCount - 1);

                // Curva de potencia suave que se usa en las barras
                float floatingIndex = (float)Math.Pow(n, 1.15) * 119;
                int idxLow = (int)floatingIndex;
                int idxHigh = Math.Min(idxLow + 1, spectrum.Length - 1);
                float frac = floatingIndex - idxLow;

                // Interpolación lineal
                float rawValue = spectrum[idxLow] * (1f - frac) + spectrum[idxHigh] * frac;

                // Ganancia dinámica idéntica a las barras
                float ganancia = 1.0f + n * 3.0f;

                // Multiplicamos por la altura disponible y el empuje del bajo
                // Subimos el tope a 0.85f y aplicamos una curva exponencial a rawValue para que los picos resalten sobre el ruido
                float targetMagnitude = (float)Math.Pow(rawValue, 0.8) * height * 0.85f * ganancia * spectrumPower;

                // Suavizado asimétrico original de tu compañero (0.55 / 0.35)
                float diff = targetMagnitude - _smoothedWavePoints[i];
                // 0.85f = Ataque casi instantáneo (salto violento). 0.45f = Caída un poco más rápida
                _smoothedWavePoints[i] += diff * (diff > 0 ? 0.85f : 0.45f);

                // Cálculo de la posición en pantalla
                float x = n * width;
                float y = groundY - _smoothedWavePoints[i];
                y = Math.Max(y, 10f); // Tope superior por seguridad

                topCurve[i] = new PointF(x, y);
            }

            using (var path = new GraphicsPath())
            {
                // Tensión de 0.4f para curvas orgánicas sin cruces raros
                path.AddCurve(topCurve, 0.4f);

                // Cerramos la figura por debajo
                path.AddLine(width, groundY, width, height);
                path.AddLine(width, height, 0, height);
                path.AddLine(0, height, 0, groundY);

                path.CloseFigure();

                // Estética reactiva
                float hueBase = (time * 40f + highEnergy * 100f) % 360f;
                Color colTop = VisualUtils.HsvToColor(hueBase, 0.9f, 1f);
                Color colBottom = VisualUtils.HsvToColor((hueBase + 60) % 360, 1f, 0.4f);

                int alphaBase = 140 + (int)(bassEnergy * 115f);

                using (var fillBrush = new LinearGradientBrush(new Point(0, 0), new Point(0, height),
                    Color.FromArgb(Math.Min(alphaBase, 255), colTop),
                    Color.FromArgb(Math.Min(alphaBase / 3, 255), colBottom)))
                {
                    g.FillPath(fillBrush, path);
                }

                _outlinePen.Color = Color.FromArgb(Math.Min(alphaBase + 50, 255), Color.White);
                g.DrawCurve(_outlinePen, topCurve, 0.4f);
            }
        }
    }
}