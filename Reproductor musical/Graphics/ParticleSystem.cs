using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reproductor_musical.Visuals
{
    public enum ParticleShape { Circle, Diamond, Line, Triangle }

    public class Particle
    {
        public float X, Y, PrevX, PrevY, VX, VY;
        public float Life, MaxLife;
        public float Size;
        public Color Color;
        public bool Active;
        public ParticleShape Shape;
        public float Rotation;
        public float RotationSpeed;
    }

    public class ParticleSystem
    {
        private readonly Particle[] _particles;
        private readonly Random _aleatorio = new Random();
        private int _siguienteEspacio;
        private float _temporizadorRafaga;

        public ParticleSystem(int maxParticles)
        {
            _particles = new Particle[maxParticles];
            for (int i = 0; i < maxParticles; i++)
                _particles[i] = new Particle();
        }

        public void Update(float bajos, float medios, float altos, int ancho, int alto, float tiempo)
        {
            float centroX = ancho / 2f, centroY = alto / 2f;

            float sensibilidadPicos = 1f; // Potencia del filtro. 1.0 = lineal, 1.5 = balanceado, 3.0 = solo picos extremos.
            float velocidadGlobal = 1.7f;   // Multiplicador general de velocidad de las partículas.
            float gravedad = 0.01f;         // Aceleración hacia abajo (ponlo negativo para que floten hacia arriba).
            float friccion = 0.990f;        // Freno del aire. 1.0f = no se frenan nunca. 0.90f = se frenan casi al instante.
                                            // ==========================================

            // 1. FILTRO MÁS SUAVE: Usamos la variable de sensibilidad en lugar de un 3 fijo
            float factorExplosion = (float)Math.Pow(bajos, sensibilidadPicos);

            // 2. FLUJO BASE CONSTANTE: Aseguramos que siempre haya partículas vivas
            int cantidadBase = (int)(bajos * 40 + medios * 15) + 1;

            // 3. RÁFAGAS RECALCULADAS: Más amigables con el ritmo general
            _temporizadorRafaga += factorExplosion * 8f;
            int emisionRafaga = (int)_temporizadorRafaga;
            _temporizadorRafaga -= emisionRafaga;

            int cantidadEmision = cantidadBase + (emisionRafaga * 20);

            for (int e = 0; e < cantidadEmision; e++)
            {
                Particle p = _particles[_siguienteEspacio % _particles.Length];
                _siguienteEspacio++;

                float angulo = (float)_aleatorio.NextDouble() * (float)Math.PI * 2;
                float dispersion = 0.5f + (float)_aleatorio.NextDouble() * 0.8f;
                float intensidad = bajos + medios + altos;

                // Aplicamos el multiplicador de VELOCIDAD GLOBAL aquí
                float velocidadBase = (float)_aleatorio.NextDouble() * 4f * intensidad + factorExplosion * 80f + altos * 20f + medios * 10f;
                float velocidadFinal = velocidadBase * velocidadGlobal;

                float distanciaLanzamiento = 20f + factorExplosion * 250f + intensidad * 60f;
                p.X = centroX + (float)Math.Cos(angulo) * distanciaLanzamiento * dispersion;
                p.Y = centroY + (float)Math.Sin(angulo) * distanciaLanzamiento * dispersion;
                p.PrevX = p.X;
                p.PrevY = p.Y;

                float desfaseRotacion = altos * 2f * (float)Math.PI;
                float anguloFinal = angulo + desfaseRotacion;

                p.VX = (float)Math.Cos(anguloFinal) * velocidadFinal;
                p.VY = (float)Math.Sin(anguloFinal) * velocidadFinal - 0.5f;

                p.MaxLife = p.Life = 80f + (float)_aleatorio.NextDouble() * 100f + altos * 60f + factorExplosion * 80f;
                p.Size = 4f + (float)_aleatorio.NextDouble() * 6f + medios * 20f + bajos * 20f;
                p.Rotation = (float)_aleatorio.NextDouble() * 360f;
                p.RotationSpeed = (float)_aleatorio.NextDouble() * 10f - 5f + altos * 30f;

                float matiz = (tiempo * 30f + _aleatorio.Next(360) + altos * 200f) % 360f;
                p.Color = VisualUtils.HsvToColor(matiz, 1f, 1f);

                float dado = (float)_aleatorio.NextDouble();
                if (dado < 0.4f) p.Shape = ParticleShape.Circle;
                else if (dado < 0.65f) p.Shape = ParticleShape.Diamond;
                else if (dado < 0.85f) p.Shape = ParticleShape.Line;
                else p.Shape = ParticleShape.Triangle;

                p.Active = true;
            }

            // ACTUALIZACIÓN FÍSICA (Usando nuestras nuevas variables)
            foreach (var p in _particles)
            {
                if (!p.Active) continue;
                p.PrevX = p.X;
                p.PrevY = p.Y;

                p.X += p.VX;
                p.Y += p.VY;

                // Aplicamos la GRAVEDAD
                p.VY += gravedad;

                // Aplicamos la FRICCIÓN (Aceleración negativa)
                p.VX *= friccion;
                p.VY *= friccion;

                p.Rotation += p.RotationSpeed;
                p.Life -= 1f;
                if (p.Life <= 0) p.Active = false;
            }
        }

        public void Draw(Graphics g, float time)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var p in _particles)
            {
                if (!p.Active) continue;
                float lifeRatio = p.Life / p.MaxLife;
                int alpha = (int)(lifeRatio * 220);
                float size = p.Size * lifeRatio;

                int glowAlpha = (int)(lifeRatio * 60);
                float glowSize = size * 2.5f;
                using (var glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, p.Color)))
                {
                    g.FillEllipse(glowBrush, p.X - glowSize / 2f, p.Y - glowSize / 2f, glowSize, glowSize);
                }

                float trailDist = Math.Abs(p.X - p.PrevX) + Math.Abs(p.Y - p.PrevY);
                if (trailDist > 1f)
                {
                    using (var trailPen = new Pen(Color.FromArgb((int)(alpha * 0.4f), p.Color), Math.Max(0.5f, size * 0.3f)))
                    {
                        g.DrawLine(trailPen, p.PrevX, p.PrevY, p.X, p.Y);
                    }
                }

                using (var brush = new SolidBrush(Color.FromArgb(alpha, p.Color)))
                {
                    switch (p.Shape)
                    {
                        case ParticleShape.Circle:
                            g.FillEllipse(brush, p.X - size / 2f, p.Y - size / 2f, size, size);
                            break;
                        case ParticleShape.Diamond:
                            var diamond = new[]
                            {
                                new PointF(p.X, p.Y - size / 2f),
                                new PointF(p.X + size / 2f, p.Y),
                                new PointF(p.X, p.Y + size / 2f),
                                new PointF(p.X - size / 2f, p.Y)
                            };
                            g.FillPolygon(brush, diamond);
                            break;
                        case ParticleShape.Line:
                            float rad = p.Rotation * (float)Math.PI / 180f;
                            float len = size * 0.8f;
                            using (var linePen = new Pen(brush, Math.Max(1f, size * 0.3f)))
                            {
                                g.DrawLine(linePen,
                                    p.X - (float)Math.Cos(rad) * len,
                                    p.Y - (float)Math.Sin(rad) * len,
                                    p.X + (float)Math.Cos(rad) * len,
                                    p.Y + (float)Math.Sin(rad) * len);
                            }
                            break;
                        case ParticleShape.Triangle:
                            float triSize = size * 0.6f;
                            float triRot = p.Rotation * (float)Math.PI / 180f;
                            var tri = new PointF[3];
                            for (int i = 0; i < 3; i++)
                            {
                                float a = triRot + (float)i / 3f * (float)Math.PI * 2;
                                tri[i] = new PointF(
                                    p.X + (float)Math.Cos(a) * triSize,
                                    p.Y + (float)Math.Sin(a) * triSize);
                            }
                            g.FillPolygon(brush, tri);
                            break;
                    }
                }
            }
        }
    }
}
