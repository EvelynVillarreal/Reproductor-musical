using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reproductor_musical.Matematica
{
    internal class TransformadaFourier
    {

        public static void Calculate(Complex[] data)
        {
            int n = data.Length;

            // 1. Reordenamiento por inversión de bits (Bit-Reversal Permutation)
            int j = 0;
            // NOTA: i < n-1, no i < n. Si i = n-1, j termina siendo n-1 y el bucle
            // interno reduce bit a 0, causando while(j >= 0) → bucle infinito.
            for (int i = 0; i < n - 1; i++)
            {
                if (i < j)
                {
                    // Intercambiamos los objetos Complex de NAudio
                    var temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }

                int bit = n >> 1;
                while (j >= bit)
                {
                    j -= bit;
                    bit >>= 1;
                }
                j += bit;
            }

            // 2. Algoritmo de Cooley-Tukey (Mariposa / Butterfly)
            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2 * Math.PI / len;

                Complex wlen = new Complex { X = (float)Math.Cos(angle), Y = (float)Math.Sin(angle) };

                for (int i = 0; i < n; i += len)
                {
                    Complex w = new Complex { X = 1f, Y = 0f };
                    int halfLen = len / 2;

                    for (int k = 0; k < halfLen; k++)
                    {
                        Complex u = data[i + k];

                        Complex t;
                        t.X = data[i + k + halfLen].X * w.X - data[i + k + halfLen].Y * w.Y;
                        t.Y = data[i + k + halfLen].X * w.Y + data[i + k + halfLen].Y * w.X;

                        // Hacemos la Suma y Resta manual para la Operación Mariposa
                        data[i + k].X = u.X + t.X;
                        data[i + k].Y = u.Y + t.Y;

                        data[i + k + halfLen].X = u.X - t.X;
                        data[i + k + halfLen].Y = u.Y - t.Y;

                        float nextWX = w.X * wlen.X - w.Y * wlen.Y;
                        w.Y = w.X * wlen.Y + w.Y * wlen.X;
                        w.X = nextWX;
                    }
                }
            }
        }

    }
}
