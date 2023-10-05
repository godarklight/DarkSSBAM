using System;
using System.Diagnostics;
using System.IO;
using GLib;

namespace DarkSSBAM
{
    class FileDriver : IDisposable
    {
        FileStream fs;
        bool running = true;
        double[] data;
        byte[] bytedata;

        //Carrier
        double phase = 0.0;

        public FileDriver(string fileName, int chunk_size)
        {
            data = new double[chunk_size];
            bytedata = new byte[chunk_size * 2];
            fs = new FileStream(fileName, FileMode.Open);
            SeekToStart();
        }

        private void SeekToStart()
        {
            byte b4 = 0;
            byte b3 = 0;
            byte b2 = 0;
            byte b1 = 0;
            while (b4 != 'd' || b3 != 'a' || b2 != 't' || b1 != 'a')
            {
                b4 = b3;
                b3 = b2;
                b2 = b1;
                int newbyte = fs.ReadByte();
                if (newbyte == -1)
                {
                    throw new IOException("WAV data not found");
                }
                else
                {
                    b1 = (byte)newbyte;
                }
            }
            fs.Seek(4, SeekOrigin.Current);
            Console.WriteLine($"Currently at: {fs.Position}");
        }

        public double[] Request()
        {
            int readBytes = fs.Read(bytedata, 0, bytedata.Length);
            if (readBytes == bytedata.Length)
            {
                for (int i = 0; i < readBytes / 2; i++)
                {
                    short s16l = (short)(bytedata[i * 2] | bytedata[i * 2 + 1] << 8);
                    data[i] = s16l / (double)short.MaxValue;
                }
                return data;
            }
            return null;
        }

        public void Dispose()
        {
            fs.Close();
        }
    }
}