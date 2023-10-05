using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Threading;
using PortAudioSharp;

namespace DarkSSBAM
{
    class Modulator : IDisposable
    {
        bool running = true;
        AutoResetEvent are = new AutoResetEvent(false);
        Thread worker;
        Complex[] fromRadio = new Complex[AudioDriver.CHUNK_SIZE * 2];
        double[] toRadio = new double[AudioDriver.CHUNK_SIZE];
        bool toRadioReady = false;
        double[] toRadioBuffer = new double[AudioDriver.CHUNK_SIZE];
        double phase = 0.0;
        double VOICE_GAIN = 0.4;
        double CARRIER_GAIN = 0.1;


        public Modulator()
        {
            worker = new Thread(new ThreadStart(WorkerThread));
            worker.Start();
        }

        public bool AudioFromRadio(double[] samples)
        {
            //Keep the last samples to help with edge effects
            for (int i = 0; i < samples.Length; i++)
            {
                fromRadio[i] = fromRadio[i + AudioDriver.CHUNK_SIZE];
                fromRadio[i + AudioDriver.CHUNK_SIZE] = samples[i];
            }
            are.Set();
            return true;
        }

        public bool AudioToRadio(double[] samples)
        {
            if (!toRadioReady)
            {
                return false;
            }
            toRadioReady = false;
            Array.Copy(toRadio, 0, samples, 0, AudioDriver.CHUNK_SIZE);
            return true;
        }

        public void Dispose()
        {
            running = false;
            worker.Join();
        }

        private void WorkerThread()
        {
            while (running)
            {
                if (are.WaitOne(1))
                {
                    //Go to the frequency domain
                    Complex[] fft = FFT.CalcFFT(fromRadio);
                    //Hilbert, delete negative frequencies, multiple positive frequencies by 2, skip DC
                    for (int i = 1; i < fft.Length; i++)
                    {
                        if (i < fft.Length / 2)
                        {
                            fft[i] = fft[i] * 2.0;
                        }
                        if (i >= fft.Length / 2)
                        {
                            fft[i] = 0.0;
                        }
                    }
                    //Go back to the time domain
                    Complex[] ifft = FFT.CalcIFFT(fft);
                    //Generate our SSB samples and 100hz carrier
                    for (int i = 0; i < AudioDriver.CHUNK_SIZE; i++)
                    {
                        //Only take the middle half to avoid FFT edge effects
                        int j = i + ifft.Length / 4;
                        double real = Math.Cos(phase) * ifft[j].Real;
                        double imaginary = Math.Sin(phase) * ifft[j].Imaginary;
                        double upperSideband = VOICE_GAIN * (real + imaginary);
                        double insertCarrier = CARRIER_GAIN * Math.Cos(phase);
                        toRadioBuffer[i] = upperSideband + insertCarrier;
                        phase += 100.0 * Math.Tau * (1.0 / 48000.0);
                        if (phase > Math.Tau)
                        {
                            phase -= Math.Tau;
                        }
                    }
                    Array.Copy(toRadioBuffer, 0, toRadio, 0, AudioDriver.CHUNK_SIZE);
                    toRadioReady = true;
                }
            }
        }
    }
}