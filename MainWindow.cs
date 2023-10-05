using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace DarkSSBAM
{
    class MainWindow : Window
    {
        [UI] private Image imgScope = null;
        [UI] private Label lblStatus = null;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);
            DeleteEvent += Window_DeleteEvent;

            lblStatus.Text = "INIT";

        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}
