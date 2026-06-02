using System;
using System.Drawing;

public class Particle
{
    public float X, Y, VX, VY;
    public float Life, MaxLife;
    public float Size;
    public Color Color;
    public bool Active;
}

public class ParticleSystem
{
    private readonly Particle[] _particles;
    private readonly Random _rng = new Random();
    private int _nextSlot;

    public ParticleSystem(int maxParticles)
    {
        _particles = new Particle[maxParticles];
        for (int i = 0; i < maxParticles; i++)
            _particles[i] = new Particle();
    }

    public void Update(float bass, float mid, int width, int height, float time)
    {
        // Emitir nuevas partículas según la energía del bajo
        int emitCount = (int)(bass * 15) + 1;
        float cx = width / 2f, cy = height / 2f;

        for (int e = 0; e < emitCount; e++)
        {
            Particle p = _particles[_nextSlot % _particles.Length];
            _nextSlot++;

            float angle = (float)_rng.NextDouble() * (float)Math.PI * 2;
            float speed = 1f + (float)_rng.NextDouble() * 4f + bass * 5f;

            p.X = cx + (float)Math.Cos(angle) * 30f;
            p.Y = cy + (float)Math.Sin(angle) * 30f;
            p.VX = (float)Math.Cos(angle) * speed;
            p.VY = (float)Math.Sin(angle) * speed - 1f; // leve gravedad inversa
            p.MaxLife = p.Life = 60f + (float)_rng.NextDouble() * 60f;
            p.Size = 2f + (float)_rng.NextDouble() * 4f + mid * 6f;
            float hue = (time * 50 + _rng.Next(360)) % 360f;
            p.Color = Visualizer.HsvToColor(hue, 1f, 1f);
            p.Active = true;
        }

        // Actualizar partículas activas
        foreach (var p in _particles)
        {
            if (!p.Active) continue;
            p.X += p.VX;
            p.Y += p.VY;
            p.VY += 0.05f;    // gravedad
            p.VX *= 0.99f;    // fricción
            p.Life -= 1f;
            if (p.Life <= 0) p.Active = false;
        }
    }

    public void Draw(Graphics g, float time)
    {
        foreach (var p in _particles)
        {
            if (!p.Active) continue;
            float lifeRatio = p.Life / p.MaxLife;
            int alpha = (int)(lifeRatio * 220);
            float size = p.Size * lifeRatio;

            using (var brush = new SolidBrush(Color.FromArgb(alpha, p.Color)))
            {
                g.FillEllipse(brush,
                    p.X - size / 2f,
                    p.Y - size / 2f,
                    size, size);
            }
        }
    }
}