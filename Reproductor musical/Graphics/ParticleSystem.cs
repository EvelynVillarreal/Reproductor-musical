using System;
using System.Drawing;
using System.Drawing.Drawing2D;

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

        // Aumentar explosividad de la emisión y empezar desde CERO
        int cantidadEmision = (int)(bajos * 80 + medios * 20);

        _temporizadorRafaga += bajos * 6f;
        int emisionRafaga = (int)_temporizadorRafaga;
        _temporizadorRafaga -= emisionRafaga;
        cantidadEmision += emisionRafaga * 10;

        for (int e = 0; e < cantidadEmision; e++)
        {
            Particle p = _particles[_siguienteEspacio % _particles.Length];
            _siguienteEspacio++;

            float angulo = (float)_aleatorio.NextDouble() * (float)Math.PI * 2;
            float dispersion = 0.5f + (float)_aleatorio.NextDouble() * 0.5f;
            float intensidad = bajos + medios + altos;
            
            // Mayor velocidad e impacto, empieza en casi cero
            float velocidad = (float)_aleatorio.NextDouble() * 2f * intensidad + bajos * 50f + altos * 20f + medios * 15f;

            // Mayor distancia de lanzamiento
            float distanciaLanzamiento = 15f + bajos * 120f + intensidad * 40f;
            p.X = centroX + (float)Math.Cos(angulo) * distanciaLanzamiento * dispersion;
            p.Y = centroY + (float)Math.Sin(angulo) * distanciaLanzamiento * dispersion;
            p.PrevX = p.X;
            p.PrevY = p.Y;

            float desfaseRotacion = altos * 2f * (float)Math.PI;
            float anguloFinal = angulo + desfaseRotacion;
            p.VX = (float)Math.Cos(anguloFinal) * velocidad;
            p.VY = (float)Math.Sin(anguloFinal) * velocidad - 0.5f;

            p.MaxLife = p.Life = 50f + (float)_aleatorio.NextDouble() * 100f + altos * 60f + bajos * 40f;
            // Mayor tamaño
            p.Size = 4f + (float)_aleatorio.NextDouble() * 5f + medios * 15f + bajos * 15f;
            p.Rotation = (float)_aleatorio.NextDouble() * 360f;
            p.RotationSpeed = (float)_aleatorio.NextDouble() * 10f - 5f + altos * 30f;

            float matiz = (tiempo * 30f + _aleatorio.Next(360) + altos * 200f) % 360f;
            p.Color = Visualizer.HsvToColor(matiz, 1f, 1f);

            float dado = (float)_aleatorio.NextDouble();
            if (dado < 0.4f) p.Shape = ParticleShape.Circle;
            else if (dado < 0.65f) p.Shape = ParticleShape.Diamond;
            else if (dado < 0.85f) p.Shape = ParticleShape.Line;
            else p.Shape = ParticleShape.Triangle;

            p.Active = true;
        }

        foreach (var p in _particles)
        {
            if (!p.Active) continue;
            p.PrevX = p.X;
            p.PrevY = p.Y;
            p.X += p.VX;
            p.Y += p.VY;
            p.VY += 0.02f;
            p.VX *= 0.99f;
            p.VY *= 0.99f;
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
