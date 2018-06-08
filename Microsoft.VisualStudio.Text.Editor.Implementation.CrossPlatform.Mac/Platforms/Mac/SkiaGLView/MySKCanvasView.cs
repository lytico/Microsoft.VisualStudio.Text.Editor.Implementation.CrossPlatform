#if __MACOS__
using System;
using System.ComponentModel;
using AppKit;
using CoreGraphics;
using Foundation;
namespace SkiaSharp.Views.Mac
{
    [Register(nameof(MySKCanvasView))]
    [DesignTimeVisible(true)]
    public class MySKCanvasView : NSView
    {
        private SKDrawable drawable;
        private bool ignorePixelScaling;

        // created in code
        public MySKCanvasView()
        {
            Initialize();
        }

        // created in code
        public MySKCanvasView(CGRect frame)
            : base(frame)
        {

            Initialize();
        }

        // created via designer
        public MySKCanvasView(IntPtr p)
            : base(p)
        {
        }

        // created via designer
        public override void AwakeFromNib()
        {
            Initialize();
        }

        private void Initialize()
        {
            drawable = new SKDrawable();
        }

        public SKSize CanvasSize => drawable.Info.Size;

        public bool IgnorePixelScaling
        {
            get { return ignorePixelScaling; }
            set
            {
                ignorePixelScaling = value;
                NeedsDisplay = true;
            }
        }

        public virtual void DrawInSurface(SKSurface surface, SKImageInfo info)
        {
            
        }

        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

            var ctx = NSGraphicsContext.CurrentContext.CGContext;

            // create the skia context
            SKImageInfo info;
            var surface = drawable.CreateSurface(Bounds, IgnorePixelScaling ? 1 : Window.BackingScaleFactor, out info);

            // draw on the image using SKiaSharp
            DrawInSurface(surface, info);

            // draw the surface to the context
            drawable.DrawSurface(ctx, Bounds, info, surface);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            drawable.Dispose();
        }
    }
}
#endif