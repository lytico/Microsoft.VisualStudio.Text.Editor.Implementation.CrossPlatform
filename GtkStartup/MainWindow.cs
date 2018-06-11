using System;
using Gtk;
using Microsoft.VisualStudio.Text.Editor;


public partial class MainWindow : Gtk.Window
{
    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Child = new Microsoft.VisualStudio.Text.Editor.SkiaTextViewHost(true);
        Build();
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
