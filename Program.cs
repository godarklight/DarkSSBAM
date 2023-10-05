using System;
using Gtk;
using System.Threading;

namespace DarkSSBAM
{
    class Program
    {
        private static bool running = true;

        [STAThread]
        public static void Main(string[] args)
        {
            Thread audioThread = new Thread(new ThreadStart(AudioThread));
            audioThread.Start();

            Application.Init();

            var app = new Application("org.DarkSignal.DarkSignal", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            var win = new MainWindow();
            app.AddWindow(win);

            win.Show();
            Application.Run();

            running = false;
            audioThread.Join();
        }

        private static void AudioThread()
        {
            AudioDriver audioDriver = new AudioDriver();
            Modulator mod = new Modulator();
            audioDriver.sinkEvent = mod.AudioToRadio;
            audioDriver.sourceEvent = mod.AudioFromRadio;
            {
                while (running)
                {
                    Thread.Sleep(1);
                }
            }
            mod.Dispose();
            audioDriver.Dispose();            
        }
    }
}
