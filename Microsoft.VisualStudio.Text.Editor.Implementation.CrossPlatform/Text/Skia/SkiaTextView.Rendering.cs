using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using SkiaSharp;

namespace Microsoft.VisualStudio.Text.Editor
{
    partial class SkiaTextView
    {
        SKFontMetrics typeMetrics;
        const int FontSize = 24;
        private void InitializeRendering()
        {
            paint.Color = SKColors.Black;
            paint.TextSize = FontSize;
            paint.Typeface = SKTypeface.FromFamilyName("Menlo");
            paint.SubpixelText = true;
            paint.LcdRenderText = true;
            paint.IsAntialias = true;
            typeMetrics = paint.FontMetrics;
            TextViewModel.VisualBuffer.Changed += VisualBuffer_Changed;
        }

        public class TextStyle
        {
            public SKPaint Paint;
            public MyShaper Shaper;
            public SKFontMetrics FontMetrics;

            public TextStyle(SKPaint paint)
            {
                this.Paint = paint;
                Shaper = new MyShaper(paint.Typeface);
                paint.GetFontMetrics(out FontMetrics);
            }
        }

        Dictionary<string, TextStyle> classifiedPaints = new Dictionary<string, TextStyle>()
        {
            [""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["comment"] = new TextStyle(new SKPaint { Color = SKColors.Gray, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["keyword"] = new TextStyle(new SKPaint { Color = SKColors.Blue, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["class name"] = new TextStyle(new SKPaint { Color = SKColors.Yellow, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["identifier"] = new TextStyle(new SKPaint { Color = SKColors.Orange, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["punctuation"] = new TextStyle(new SKPaint { Color = SKColors.AliceBlue, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["operator"] = new TextStyle(new SKPaint { Color = SKColors.AliceBlue, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["number"] = new TextStyle(new SKPaint { Color = SKColors.Purple, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["string"] = new TextStyle(new SKPaint { Color = SKColors.Brown, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            ["interface name"] = new TextStyle(new SKPaint { Color = SKColors.GreenYellow, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
        };

        List<TextContentChangedEventArgs> changes = new List<TextContentChangedEventArgs>();
        void VisualBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            INormalizedTextChangeCollection a = e.Changes;
            changes.Add(e);
            // NeedsDisplay = true;
        }


        SKPaint paint = new SKPaint();
        List<ITextSnapshotLine> lines = new List<ITextSnapshotLine>();
        //public override void DrawInSurface(SKSurface surface, GRBackendRenderTargetDesc renderTarget)
        //{
        //    ViewportWidth = Bounds.Width * 2;
        //    ViewportHeight = Bounds.Height * 2;
        //    sw.Restart();
        //    LayoutLines();
        //    Console.WriteLine("RS" + sw.Elapsed);
        //    sw.Restart();
        //    surface.Canvas.Clear(SKColors.White);
        //    DrawLines(surface.Canvas);
        //    ((SkiaTextCaret)Caret).OnRender(surface.Canvas);
        //    Console.WriteLine("RE" + sw.Elapsed);
        //}

        //List<(int line, double height)> lineHeights;
        ITextSnapshot lastLayoutSnapshot;
        List<SkiaTextViewLine> createdLines;
        private void LayoutLines()
        {
            if (createdLines == null)
            {
                createdLines = new List<SkiaTextViewLine>();
                foreach (var line in this.VisualSnapshot.Lines)
                {
                    SkiaTextViewLine item = new SkiaTextViewLine();
                    createdLines.Add(item);
                    item.Update(line, classifiedPaints);
                    MaxTextRightCoordinate = Math.Max(item.TextWidth, MaxTextRightCoordinate);
                }
                textViewLines = new SkiaTextViewLineCollection(this, createdLines);
                Caret.MoveTo(createdLines[5], 100);
            }
        }

        private void DrawLines(SKCanvas canvas)
        {
            SKPoint point = new SKPoint((float)-ViewportLeft, (float)-ViewportTop);
            foreach (var line in createdLines)
            {
                if (-ViewportTop > point.Y)
                {
                    point.Y += (float)line.Height;
                    continue;
                }
                line.Render(canvas, point);
                if (point.Y > ViewportBottom)
                    break;
                point.Y += (float)line.Height;
            }
        }

    }
}
