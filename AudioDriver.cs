using System;
using System.Diagnostics.Tracing;
using System.Numerics;
using System.Threading;
using PortAudioSharp;

namespace DarkSSBAM
{
    class AudioDriver : IDisposable
    {
        public const int CHUNK_SIZE = 512;
        Stream audioStream;
        AutoResetEvent are = new AutoResetEvent(false);
        int sourcePos = 0;
        int sinkPos = 0;
        bool sourceOk = false;
        bool sinkOk = false;
        public Func<double[], bool> sourceEvent;
        double[] sourceBuffer = new double[CHUNK_SIZE];
        double[] sinkBuffer = new double[CHUNK_SIZE];
        public Func<double[], bool> sinkEvent;
        bool running = true;

        //Carrier
        double phase = 0.0;

        public AudioDriver()
        {
            PortAudio.Initialize();
            DeviceInfo di = PortAudio.GetDeviceInfo(PortAudio.DefaultInputDevice);
            Console.WriteLine($"Reading from {di.name}");
            StreamParameters inParam = new StreamParameters();
            inParam.channelCount = 1;
            inParam.device = PortAudio.DefaultInputDevice;
            inParam.sampleFormat = SampleFormat.Int16;
            inParam.suggestedLatency = 0.01;
            StreamParameters outParam = new StreamParameters();
            outParam.channelCount = 1;
            outParam.device = PortAudio.DefaultOutputDevice;
            outParam.sampleFormat = SampleFormat.Int16;
            outParam.suggestedLatency = 0.01;
            audioStream = new Stream(inParam, outParam, 48000, 0, StreamFlags.NoFlag, AudioCallback, null);
            audioStream.Start();
            int defaultDevice = PortAudio.DefaultInputDevice;
        }

        public StreamCallbackResult AudioCallback(IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userDataPtr)
        {
            if (!running)
            {
                return StreamCallbackResult.Complete;
            }
            unsafe
            {
                short* sourceptr = (short*)input.ToPointer();
                short* sinkptr = (short*)output.ToPointer();
                for (int i = 0; i < frameCount; i++)
                {
                    //Source
                    short sourceS16 = *sourceptr++;
                    sourceBuffer[sourcePos++] = sourceS16 / (double)short.MaxValue;
                    if (sourcePos == CHUNK_SIZE)
                    {
                        if (sourceEvent != null)
                        {
                            bool newSourceOk = sourceEvent(sourceBuffer);
                            if (sourceOk && !newSourceOk)
                            {
                                Console.WriteLine("Source Samples Lost");
                            }
                            newSourceOk = sourceOk;
                        }
                        sourcePos = 0;
                    }

                    //Sink
                    short sinkS16 = (short)(sinkBuffer[sinkPos++] * short.MaxValue);
                    *sinkptr++ = sinkS16;
                    if (sinkPos == CHUNK_SIZE)
                    {
                        if (sinkEvent != null)
                        {
                            bool newSinkOK = sinkEvent(sinkBuffer);
                            if (sinkOk && !newSinkOK)
                            {
                                Console.WriteLine("Sink Samples Lost");
                                Array.Clear(sinkBuffer);
                            }
                            sinkOk = newSinkOK;
                        }
                        sinkPos = 0;
                    }
                }
            }
            return StreamCallbackResult.Continue;
        }

        public void Dispose()
        {
            running = false;
            audioStream.Stop();
        }
    }
}