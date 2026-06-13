using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public class WaveCircleRenderer
    {
        public void Render(Graphics g, int width, int height, float[] spectrum, float bassEnergy, float midEnergy, float highEnergy, float time)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float centroX = width / 2f, centroY = height / 2f;
            float radioBase = Math.Min(width, height) * 0.2f;
            float radioPulso = radioBase + bassEnergy * 300f;

            int cantidadPuntos = 256;
            PointF[] puntosExteriores = new PointF[cantidadPuntos];
            PointF[] puntosMedios = new PointF[cantidadPuntos];
            PointF[] puntosInteriores = new PointF[cantidadPuntos];

            float matizBase = (time * 30f) % 360f;

            for (int i = 0; i < cantidadPuntos; i++)
            {
                float angulo = (float)i / cantidadPuntos * (float)(Math.PI * 2);
                float onda = spectrum[i] * 350f;
                float ondaMedia = spectrum[(i + 50) % spectrum.Length] * 150f;
                float ondaAlta = spectrum[(i + 100) % spectrum.Length] * 80f;

                float rExterior = radioPulso + onda;
                float rMedio = radioPulso * 0.65f + ondaMedia + bassEnergy * 80f;
                float rInterior = radioPulso * 0.3f + ondaAlta + midEnergy * 50f;

                puntosExteriores[i] = new PointF(centroX + (float)Math.Cos(angulo) * rExterior,
                                                 centroY + (float)Math.Sin(angulo) * rExterior);
                puntosMedios[i] = new PointF(centroX + (float)Math.Cos(angulo) * rMedio,
                                             centroY + (float)Math.Sin(angulo) * rMedio);
                puntosInteriores[i] = new PointF(centroX + (float)Math.Cos(angulo) * rInterior,
                                                 centroY + (float)Math.Sin(angulo) * rInterior);
            }

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(puntosExteriores);
                using (var brush = new SolidBrush(Color.FromArgb(60, VisualUtils.HsvToColor(matizBase, 0.8f, 1f))))
                    g.FillPath(brush, path);
                using (var pen = new Pen(VisualUtils.HsvToColor(matizBase, 0.9f, 1f), 2f))
                    g.DrawPolygon(pen, puntosExteriores);
            }

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(puntosMedios);
                using (var brush = new SolidBrush(Color.FromArgb(80, VisualUtils.HsvToColor((matizBase + 120) % 360, 0.8f, 1f))))
                    g.FillPath(brush, path);
                using (var pen = new Pen(VisualUtils.HsvToColor((matizBase + 120) % 360, 0.9f, 1f), 1.5f))
                    g.DrawPolygon(pen, puntosMedios);
            }

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(puntosInteriores);
                using (var brush = new SolidBrush(Color.FromArgb(100, VisualUtils.HsvToColor((matizBase + 240) % 360, 0.8f, 1f))))
                    g.FillPath(brush, path);
                using (var pen = new Pen(VisualUtils.HsvToColor((matizBase + 240) % 360, 0.9f, 1f), 1.5f))
                    g.DrawPolygon(pen, puntosInteriores);
            }

            int cantidadPicos = 36;
            for (int i = 0; i < cantidadPicos; i++)
            {
                float angulo = (float)i / cantidadPicos * (float)(Math.PI * 2) + time * 0.5f;
                int indiceEspec = (i * 7) % spectrum.Length;
                float longitudPico = spectrum[indiceEspec] * 250f + highEnergy * 100f;
                float longitudInterior = 5f + bassEnergy * 80f;

                PointF p1 = new PointF(centroX + (float)Math.Cos(angulo) * longitudInterior,
                                       centroY + (float)Math.Sin(angulo) * longitudInterior);
                PointF p2 = new PointF(centroX + (float)Math.Cos(angulo) * (radioPulso * 0.3f + longitudPico),
                                       centroY + (float)Math.Sin(angulo) * (radioPulso * 0.3f + longitudPico));
                float matiz = (matizBase + i * 10f) % 360f;
                using (var pen = new Pen(Color.FromArgb(100, VisualUtils.HsvToColor(matiz, 1f, 1f)), 2.5f))
                    g.DrawLine(pen, p1, p2);
            }

            float tamanoCentroOnda = radioPulso * 0.2f + bassEnergy * 100f + highEnergy * 40f;
            using (var pincelCentro = new SolidBrush(Color.FromArgb(180, VisualUtils.HsvToColor((matizBase + 180) % 360, 1f, 1f))))
            {
                g.FillEllipse(pincelCentro,
                    centroX - tamanoCentroOnda / 2f, centroY - tamanoCentroOnda / 2f,
                    tamanoCentroOnda, tamanoCentroOnda);
            }

            int cantidadPuntosFlotantes = 12;
            for (int i = 0; i < cantidadPuntosFlotantes; i++)
            {
                float angulo = (float)i / cantidadPuntosFlotantes * (float)(Math.PI * 2) + time * (1f + midEnergy * 2f);
                float radioOrbita = radioPulso * 0.7f + spectrum[(i * 20) % spectrum.Length] * 100f;
                float tamanoPunto = 4f + spectrum[(i * 15) % spectrum.Length] * 30f;
                PointF posPunto = new PointF(
                    centroX + (float)Math.Cos(angulo) * radioOrbita,
                    centroY + (float)Math.Sin(angulo) * radioOrbita);
                float matizPunto = (matizBase + i * 30f) % 360f;
                using (var pincelPunto = new SolidBrush(VisualUtils.HsvToColor(matizPunto, 1f, 1f)))
                    g.FillEllipse(pincelPunto, posPunto.X - tamanoPunto / 2, posPunto.Y - tamanoPunto / 2, tamanoPunto, tamanoPunto);
            }
        }
    }
}
