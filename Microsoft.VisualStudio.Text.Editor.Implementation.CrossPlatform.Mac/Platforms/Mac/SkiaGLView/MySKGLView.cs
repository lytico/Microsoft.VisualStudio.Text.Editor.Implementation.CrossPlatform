﻿#if __MACOS__
using System;
using System.ComponentModel;
using AppKit;
using CoreGraphics;
using Foundation;
using SkiaSharp.Views.GlesInterop;

namespace SkiaSharp.Views.Mac
{
    [Register(nameof(MySKGLView))]
    [DesignTimeVisible(true)]
    public class MySKGLView : NSOpenGLView
    {
        private GRContext context;
        private GRBackendRenderTargetDesc renderTarget;

        // created in code
        public MySKGLView()
        {
            Initialize();
        }

        // created in code
        public MySKGLView(CGRect frame)
            : base(frame)
        {
            Initialize();
        }

        // created via designer
        public MySKGLView(IntPtr p)
            : base(p)
        {
        }

        public override bool IsOpaque => true;

        // created via designer
        public override void AwakeFromNib()
        {
            Initialize();
        }

        private void Initialize()
        {
            WantsBestResolutionOpenGLSurface = true;

            var attrs = new NSOpenGLPixelFormatAttribute[]
            {
                //NSOpenGLPixelFormatAttribute.OpenGLProfile, (NSOpenGLPixelFormatAttribute)NSOpenGLProfile.VersionLegacy,
                NSOpenGLPixelFormatAttribute.Accelerated,
                NSOpenGLPixelFormatAttribute.DoubleBuffer,
                NSOpenGLPixelFormatAttribute.Multisample,

                NSOpenGLPixelFormatAttribute.ColorSize, (NSOpenGLPixelFormatAttribute)32,
                NSOpenGLPixelFormatAttribute.AlphaSize, (NSOpenGLPixelFormatAttribute)8,
                NSOpenGLPixelFormatAttribute.DepthSize, (NSOpenGLPixelFormatAttribute)24,
                NSOpenGLPixelFormatAttribute.StencilSize, (NSOpenGLPixelFormatAttribute)8,
                NSOpenGLPixelFormatAttribute.SampleBuffers, (NSOpenGLPixelFormatAttribute)1,
                NSOpenGLPixelFormatAttribute.Samples, (NSOpenGLPixelFormatAttribute)4,
                (NSOpenGLPixelFormatAttribute)0,
            };
            PixelFormat = new NSOpenGLPixelFormat(attrs);
        }

        public SKSize CanvasSize => new SKSize(renderTarget.Width, renderTarget.Height);

        public override void PrepareOpenGL()
        {
            base.PrepareOpenGL();

            // create the context
            var glInterface = GRGlInterface.CreateNativeGlInterface();
            context = GRContext.Create(GRBackend.OpenGL, glInterface);

            renderTarget = SKGLDrawable.CreateRenderTarget();
        }

        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

            var size = ConvertSizeToBacking(Bounds.Size);
            renderTarget.Width = (int)size.Width;
            renderTarget.Height = (int)size.Height;

            Gles.glClear(Gles.GL_STENCIL_BUFFER_BIT);

            using (var surface = SKSurface.Create(context, renderTarget))
            {
                // draw on the surface
                DrawInSurface(surface, renderTarget);

                surface.Canvas.Flush();
            }

            // flush the SkiaSharp contents to GL
            context.Flush();

            OpenGLContext.FlushBuffer();
        }

        public virtual void DrawInSurface(SKSurface surface, GRBackendRenderTargetDesc renderTarget)
        {
        }
    }
}
#endif