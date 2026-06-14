using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class OscilloscopeRenderer
    {
        private const int PointCount = 200;
        private readonly float[] _smoothedX = new float[PointCount];
        private readonly float[] _smoothedY = new float[PointCount];

        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float highEnergy, float time)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = width / 2f;
            float cy = height / 2f;
            float maxRadius = Math.Min(width, height) * 0.4f;

            PointF[] points = new PointF[PointCount];

            // Frecuencias base para la figura de Lissajous estática
            // Al cambiar estos multiplicadores (ej. 3 y 2), la figura base cambia de forma
            float freqX = 3f;
            float freqY = 2f;

            for (int i = 0; i < PointCount; i++)
            {
                // Mapeamos el índice a un ángulo en radianes
                float t = (float)i / PointCount * (float)Math.PI * 2f;

                // Tomamos un valor de los graves para afectar el eje X
                int bassIndex = (i * 2) % 40; // Rango de bajos
                float modX = 1f + spectrum[bassIndex] * 2.5f;

                // Tomamos un valor de los agudos para afectar el eje Y
                int highIndex = 80 + (i * 3) % 100; // Rango de medios-altos
                float modY = 1f + spectrum[highIndex] * 2.5f;

                // Ecuación paramétrica de Lissajous alterada por el audio
                float targetX = cx + (float)Math.Sin(t * freqX + time * 2f) * maxRadius * modX;
                float targetY = cy + (float)Math.Cos(t * freqY - time * 1.5f) * maxRadius * modY;

                // Inercia para que el rayo fluya y no tiemble erráticamente
                float diffX = targetX - _smoothedX[i];
                float diffY = targetY - _smoothedY[i];

                _smoothedX[i] += diffX * 0.4f;
                _smoothedY[i] += diffY * 0.4f;

                points[i] = new PointF(_smoothedX[i], _smoothedY[i]);
            }

            // --- RENDERIZADO ESTILO CRT (Tubo de Rayos Catódicos) ---

            // Color base: Verde clásico de osciloscopio, virando a cian en los picos altos
            float hue = (120f + highEnergy * 60f) % 360f;
            Color crtColor = VisualUtils.HsvToColor(hue, 1f, 1f);

            using (var path = new GraphicsPath())
            {
                // Conectamos los puntos en una curva cerrada
                path.AddClosedCurve(points, 0.5f);

                // Capa 1: Resplandor exterior difuso
                using (var penGlowOut = new Pen(Color.FromArgb(20, crtColor), 15f + bassEnergy * 20f))
                    g.DrawPath(penGlowOut, path);

                // Capa 2: Resplandor interior más denso
                using (var penGlowIn = new Pen(Color.FromArgb(60, crtColor), 6f + bassEnergy * 10f))
                    g.DrawPath(penGlowIn, path);

                // Capa 3: El "haz de electrones" central, sólido y brillante
                using (var penCore = new Pen(Color.FromArgb(255, Color.White), 2f))
                    g.DrawPath(penCore, path);
            }
        }
    }
}