using System;
using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Microsoft.VisualStudio.Text.Editor
{
    public class MyShaper : IDisposable
    {
        internal const int FONT_SIZE_SCALE = 512;

        private Font font;
        private HarfBuzzSharp.Buffer buffer;
        private int TabSize = 4;

        public MyShaper(SKTypeface typeface)
        {
            if (typeface == null)
                throw new ArgumentNullException(nameof(typeface)); ;
            Typeface = typeface;
            int index;
            using (var blob = Typeface.OpenStream(out index).ToHarfBuzzBlob())
            using (var face = new Face(blob, (uint)index))
            {
                face.Index = (uint)index;
                face.UnitsPerEm = (uint)Typeface.UnitsPerEm;

                font = new Font(face);
                font.SetScale(FONT_SIZE_SCALE, FONT_SIZE_SCALE);
#if __MAC__
                font.SetFunctionsOpenType();
#endif
            }

            buffer = new HarfBuzzSharp.Buffer();
        }

        public SKTypeface Typeface { get; private set; }

        public void Dispose()
        {
            font?.Dispose();
            buffer?.Dispose();
        }

        public Result Shape(string text, SKPoint offset, SKPaint paint)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));

            if (string.IsNullOrEmpty(text))
                return new Result();

            // add the text to the buffer
            buffer.ClearContents();
            buffer.AddUtf8(text);
            Console.WriteLine("text :" + text);
            // try to understand the text
            buffer.GuessSegmentProperties();

            // do the shaping
            font.Shape(buffer);

            // get the shaping results
            var len = buffer.Length;
            var info = buffer.GlyphInfos;
            var pos = buffer.GlyphPositions;

            // get the sizes
            float textSizeY = paint.TextSize / FONT_SIZE_SCALE;
            float textSizeX = 10 * paint.TextScaleX;

            var points = new SKPoint[len];
            var clusters = new uint[len];
            var codepoints = new uint[len];
            int lastTabIndex = -1;


            for (var i = 0; i < len; i++)
            {
                if (info[i].Codepoint == 0 && text[i] == '\t')
                {
                    var distanceFromLastTab = i - lastTabIndex - 1;
                    lastTabIndex = i;
                    pos[i].XAdvance *= TabSize - distanceFromLastTab % TabSize;
                }
                codepoints[i] = info[i].Codepoint;

                clusters[i] = info[i].Cluster;

                points[i] = new SKPoint(
                    offset.X + pos[i].XOffset * textSizeX,
                    offset.Y - pos[i].YOffset * textSizeY
                );
                Console.WriteLine("point :" + points[i].X +"x"+ points[i].Y);

                // move the cursor
                offset.X += pos[i].XAdvance * textSizeX;
                offset.Y += pos[i].YAdvance * textSizeY;
            }

            return new Result(codepoints, clusters, points, text, offset);
        }

        public class Result
        {
            public Result()
            {
                Codepoints = new uint[0];
                Clusters = new uint[0];
                Points = new SKPoint[0];
                Text = "";
            }

            public readonly SKPoint EndPoint;

            public Result(uint[] codepoints, uint[] clusters, SKPoint[] points, string text, SKPoint endPoint)
            {
                this.EndPoint = endPoint;
                Text = text;
                Codepoints = codepoints;
                Clusters = clusters;
                Points = points;
            }

            public uint[] Codepoints { get; private set; }

            public uint[] Clusters { get; private set; }

            public SKPoint[] Points { get; private set; }

            public string Text { get; }

        }
    }
}
