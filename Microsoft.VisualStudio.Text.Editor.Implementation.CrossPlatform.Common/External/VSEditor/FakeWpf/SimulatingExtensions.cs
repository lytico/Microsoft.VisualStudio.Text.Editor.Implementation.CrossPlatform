using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Windows.Input
{
    static class MouseSimulatingExtensions
    {
#if __GTK__
        public static bool IsMouseOver (this Gtk.Widget widget)
        {
            widget.GetPointer (out int x, out int y);
            return x >= 0 && y >= 0 && x <= widget.Allocation.Width && y <= widget.Allocation.Height;
		}
#endif
        public static bool IsMouseOver(this Xwt.Widget widget)
        {
            var mousePosition = Xwt.Desktop.MouseLocation;
            return widget.ScreenBounds.Contains(mousePosition);
        }

        public static bool IsMouseOver(this Xwt.Window widget)
        {
            var mousePosition = Xwt.Desktop.MouseLocation;
            return widget.ScreenBounds.Contains(mousePosition);
        }

#if __MACOS__
        public static void GetPointer(this AppKit.NSView widget, out int x, out int y)
        {
            // TODO
            x = y = 0;
        }
#endif
    }
}
