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
            float intensity = bass + mid + high;
            float speed = 2f + (float)_rng.NextDouble() * 5f + bass * 18f + high * 8f + mid * 4f;

            float launchDist = 15f + bass * 50f + intensity * 20f;
            p.X = cx + (float)Math.Cos(angle) * launchDist * spread;
            p.Y = cy + (float)Math.Sin(angle) * launchDist * spread;
            p.PrevX = p.X;
            p.PrevY = p.Y;

            float rotOffset = high * 2f * (float)Math.PI;
            float finalAngle = angle + rotOffset;
            p.VX = (float)Math.Cos(finalAngle) * speed;
            p.VY = (float)Math.Sin(finalAngle) * speed - 0.5f;

            p.MaxLife = p.Life = 50f + (float)_rng.NextDouble() * 100f + high * 40f + bass * 20f;
            p.Size = 3f + (float)_rng.NextDouble() * 4f + mid * 10f + bass * 4f;
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
