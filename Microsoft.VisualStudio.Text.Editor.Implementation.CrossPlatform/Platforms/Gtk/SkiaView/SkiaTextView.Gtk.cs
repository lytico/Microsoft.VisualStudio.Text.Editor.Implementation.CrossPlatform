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
using SkiaSharp.Views.Desktop;

namespace Microsoft.VisualStudio.Text.Editor
{
    partial class SkiaTextView : EventBox
    {
        global::SkiaSharp.Views.Gtk.SKWidget skiaView;

        public SkiaTextView()
        {
            this.skiaView = new global::SkiaSharp.Views.Gtk.SKWidget();
            this.Child = skiaView;
            //this.PackStart(skiaView, true, true, 01);
            skiaView.PaintSurface += OnPaintSurface;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // the the canvas and properties
            var canvas = e.Surface.Canvas;
            /*
            // get the screen density for scaling
            var scale = 1f;
            var scaledSize = new SKSize(e.Info.Width / scale, e.Info.Height / scale);
            // handle the device screen density
            canvas.Scale(scale);

            // make sure the canvas is blank
            canvas.Clear(SKColors.Red);

            // draw some text
            var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                TextAlign = SKTextAlign.Center,
                TextSize = 24
            };
            var coord = new SKPoint(scaledSize.Width / 2, (scaledSize.Height + paint.TextSize) / 2);
            canvas.DrawText("SkiaSharp", coord, paint);*/
            LayoutLines();
            ViewportWidth = Allocation.Width * 2;
            ViewportHeight = Allocation.Height * 2;
            LayoutLines();
            canvas.Clear(SKColors.White);
            DrawLines(canvas);
            ((SkiaTextCaret)Caret).OnRender(canvas);
        }
        /*
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
        }*/

    }
}
#endif