using System;
using System.Drawing;
using System.Drawing.Drawing2D;

public enum ParticleShape { Circle, Diamond, Line, Triangle }

public class Particle
{
    public float X, Y, VX, VY;
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
    private readonly Random _rng = new Random();
    private int _nextSlot;
    private float _burstTimer;

    public ParticleSystem(int maxParticles)
    {
        _particles = new Particle[maxParticles];
        for (int i = 0; i < maxParticles; i++)
            _particles[i] = new Particle();
    }

    public void Update(float bass, float mid, float high, int width, int height, float time)
    {
        float cx = width / 2f, cy = height / 2f;

        int emitCount = (int)(bass * 20 + mid * 5) + 1;

        _burstTimer += bass * 2f;
        int burstEmit = (int)_burstTimer;
        _burstTimer -= burstEmit;
        emitCount += burstEmit * 5;

        for (int e = 0; e < emitCount; e++)
        {
            Particle p = _particles[_nextSlot % _particles.Length];
            _nextSlot++;

            float angle = (float)_rng.NextDouble() * (float)Math.PI * 2;
            float spread = 0.5f + (float)_rng.NextDouble() * 0.5f;
            float speed = 1f + (float)_rng.NextDouble() * 3f + bass * 8f + high * 3f;

            p.X = cx + (float)Math.Cos(angle) * 20f * spread;
            p.Y = cy + (float)Math.Sin(angle) * 20f * spread;

            float rotOffset = high * 2f * (float)Math.PI;
            float finalAngle = angle + rotOffset;
            p.VX = (float)Math.Cos(finalAngle) * speed;
            p.VY = (float)Math.Sin(finalAngle) * speed - 0.5f;

            p.MaxLife = p.Life = 40f + (float)_rng.NextDouble() * 80f + high * 30f;
            p.Size = 1.5f + (float)_rng.NextDouble() * 3f + mid * 8f + bass * 3f;
            p.Rotation = (float)_rng.NextDouble() * 360f;
            p.RotationSpeed = (float)_rng.NextDouble() * 10f - 5f + high * 20f;

            float hue = (time * 30f + _rng.Next(360) + high * 200f) % 360f;
            p.Color = Visualizer.HsvToColor(hue, 1f, 1f);

            float roll = (float)_rng.NextDouble();
            if (roll < 0.4f) p.Shape = ParticleShape.Circle;
            else if (roll < 0.65f) p.Shape = ParticleShape.Diamond;
            else if (roll < 0.85f) p.Shape = ParticleShape.Line;
            else p.Shape = ParticleShape.Triangle;

            p.Active = true;
        }

        foreach (var p in _particles)
        {
            if (!p.Active) continue;
            p.X += p.VX;
            p.Y += p.VY;
            p.VY += 0.03f;
            p.VX *= 0.98f;
            p.VY *= 0.98f;
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
