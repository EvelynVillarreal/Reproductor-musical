using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class GeometricPulseRenderer
    {
        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float midEnergy, float highEnergy, float time, float smoothedBass, float smoothedMid, float smoothedHigh)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float centroX = width / 2f, centroY = height / 2f;
            float radioMaximo = Math.Min(width, height) * 0.35f;

            for (int capa = 0; capa < 6; capa++)
            {
                float escalaCapa = 1f - capa * 0.12f;
                float radio = radioMaximo * escalaCapa * (1f + bassEnergy * 1.5f);

                int puntosGeometria = 4 + capa * 2 + (int)(smoothedHigh * 8f);

                float rotacion = time * (20f + smoothedMid * 30f) * (capa % 2 == 0 ? 1 : -1) + capa * 30f;
                float matiz = (capa * 50f + time * 40f + smoothedHigh * 100f) % 360f;
                int opacidad = 100 + (int)(smoothedBass * 100f);

                PointF[] estrella = VisualUtils.StarPolygon(centroX, centroY, radio * 0.5f, radio, puntosGeometria, rotacion);

                using (var pincelGordo = new Pen(Color.FromArgb(Math.Min(opacidad, 255), VisualUtils.HsvToColor(matiz, 1f, 1f)), 3f + smoothedBass * 4f))
                {
                    g.DrawPolygon(pincelGordo, estrella);
                }

                if (capa > 0 && capa % 2 == 0)
                {
                    int totalVertices = puntosGeometria * 2;
                    for (int i = 0; i < totalVertices; i++)
                    {
                        int siguiente = (i + 2) % totalVertices;
                        float matizLinea = (matiz + i * 10f) % 360f;

                        using (var pincelDelgado = new Pen(Color.FromArgb(120, VisualUtils.HsvToColor(matizLinea, 0.8f, 1f)), 2f + smoothedMid * 1.5f))
                        {
                            g.DrawLine(pincelDelgado, estrella[i], estrella[siguiente]);
                        }
                    }
                }
            }

            float tamanoCentro = radioMaximo * 0.15f * (1f + bassEnergy * 0.8f);
            float matizCentro = (time * 60f + smoothedHigh * 200f) % 360f;
            using (var pincelCentro = new SolidBrush(Color.FromArgb(200, VisualUtils.HsvToColor(matizCentro, 1f, 1f))))
            {
                g.FillEllipse(pincelCentro,
                    centroX - tamanoCentro / 2f, centroY - tamanoCentro / 2f,
                    tamanoCentro, tamanoCentro);
            }

            int cantidadOrbitas = 8 + (int)(smoothedMid * 12f);
            PointF[] puntosOrbita = new PointF[cantidadOrbitas];
            for (int i = 0; i < cantidadOrbitas; i++)
            {
                float angulo = (float)i / cantidadOrbitas * (float)(Math.PI * 2) + time * (1f + smoothedMid);
                float distanciaOrbita = radioMaximo * 0.6f * (1f + spectrum[(i * 10) % spectrum.Length] * 0.5f) * (1f + bassEnergy * 0.3f);
                float tamanoPunto = 4f + smoothedHigh * 10f;
                float matizOrbita = (matizCentro + i * (360f / cantidadOrbitas)) % 360f;

                puntosOrbita[i] = new PointF(
                    centroX + (float)Math.Cos(angulo) * distanciaOrbita,
                    centroY + (float)Math.Sin(angulo) * distanciaOrbita);

                using (var pincelOrbita = new SolidBrush(VisualUtils.HsvToColor(matizOrbita, 1f, 1f)))
                {
                    g.FillEllipse(pincelOrbita, puntosOrbita[i].X - tamanoPunto / 2, puntosOrbita[i].Y - tamanoPunto / 2, tamanoPunto, tamanoPunto);
                }
            }

            if (cantidadOrbitas > 2)
            {
                using (var pincelRed = new Pen(Color.FromArgb(90, Color.White), 1.5f))
                {
                    g.DrawPolygon(pincelRed, puntosOrbita);
                }
            }
        }
    }
}
