//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//

#if __GTK__
using System;
using System.Linq;
using Gdk;
using Gtk;
using Cairo;
using SkiaSharp;

namespace Microsoft.VisualStudio.Text.Editor
{
    partial class SkiaTextView : DrawingArea
    {
          
        public SkiaTextView(IntPtr p) : base(p)
        {
        }


        protected override bool OnExposeEvent(EventExpose evnt)
        {
            const int width = 100;
            const int height = 100;

            using (var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul))
            {
                IntPtr len;
                using (var skSurface = SKSurface.Create(bitmap.Info.Width, bitmap.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul, bitmap.GetPixels(out len), bitmap.Info.RowBytes))
                {
                    var canvas = skSurface.Canvas;
                    canvas.Clear(SKColors.White);

                    using (var paint = new SKPaint())
                    {
                        paint.StrokeWidth = 4;
                        paint.Color = new SKColor(0x2c, 0x3e, 0x50);

                        var rect = new SKRect(10, 10, 50, 50);
                        canvas.DrawRect(rect, paint);
                    }

                    Cairo.Surface surface = new Cairo.ImageSurface(
                        bitmap.GetPixels(out len),
                        Cairo.Format.Argb32,
                        bitmap.Width, bitmap.Height,
                        bitmap.Width * 4);


                    surface.MarkDirty();
                    using (var cr = CairoHelper.Create(evnt.Window))
                    {
                        cr.SetSourceSurface(surface, 0, 0);
                        cr.Paint();
                    }
                }
            }

            return base.OnExposeEvent(evnt);
        }

    }
}
#endif