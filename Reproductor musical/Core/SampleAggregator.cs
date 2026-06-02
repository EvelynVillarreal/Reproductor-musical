using NAudio.Dsp;
using NAudio.Wave;
using System;

public class SampleAggregator : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _fftLength;
    private readonly Complex[] _fftBuffer;
    private int _fftPos;

    public bool PerformFFT { get; set; }
    public event EventHandler<FftEventArgs> FftCalculated;
    public WaveFormat WaveFormat => _source.WaveFormat;

    public SampleAggregator(ISampleProvider source, int fftLength = 2048)
    {
        _source = source;
        _fftLength = fftLength;
        _fftBuffer = new Complex[fftLength];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);
        if (PerformFFT && FftCalculated != null)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                // Ventana de Hann para reducir spectral leakage
                double window = 0.5 * (1 - Math.Cos(2 * Math.PI * _fftPos / (_fftLength - 1)));
                _fftBuffer[_fftPos].X = (float)(buffer[offset + i] * window);
                _fftBuffer[_fftPos].Y = 0;
                _fftPos++;

                if (_fftPos >= _fftLength)
                {
                    FastFourierTransform.FFT(true, (int)Math.Log(_fftLength, 2), _fftBuffer);
                    FftCalculated?.Invoke(this, new FftEventArgs(_fftBuffer));
                    _fftPos = 0;
                }
            }
        }
        return samplesRead;
    }
}

public class FftEventArgs : EventArgs
{
    public Complex[] Result { get; }
    public FftEventArgs(Complex[] result) => Result = result;
}

