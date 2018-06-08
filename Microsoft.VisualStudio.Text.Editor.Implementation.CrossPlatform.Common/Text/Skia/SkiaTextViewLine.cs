using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using SkiaSharp;

namespace Microsoft.VisualStudio.Text.Editor
{
    class SkiaTextViewLine : ITextViewLine
    {
        public class RenderPart
        {
            public SKPaint Paint;
            public MyShaper.Result Text;
        }
        public List<RenderPart> RenderList = new List<RenderPart>();

        ITextSnapshot sourceTextSnapshot;
        SnapshotSpan _extentIncludingLineBreak;
        IBufferGraph _bufferGraph;
        int _lineBreakLength;
        ITextSnapshotLine line;

        public void Update(ITextSnapshotLine line, Dictionary<string, SkiaTextView.TextStyle> styles)
        {
            var defaultStle = styles[""];
            RenderList.Clear();
            SKPoint point = new SKPoint();
            Height = 0;
            sourceTextSnapshot = line.Snapshot;
            this.line = line;
            _extentIncludingLineBreak = line.ExtentIncludingLineBreak;
            _lineBreakLength = line.LineBreakLength;
            //var classList = new List<ClassifiedSpan>();
            //int lastOffset = line.Start;
            //foreach (var item in classified)
            //{
            //    if (lastOffset < item.TextSpan.Start)
            //    {
            //        classList.Add(new ClassifiedSpan("", new Microsoft.CodeAnalysis.Text.TextSpan(lastOffset, item.TextSpan.Start - lastOffset)));
            //    }
            //    classList.Add(item);
            //    lastOffset = item.TextSpan.End;
            //}
            //if (lastOffset < line.End)
            //{
            //    classList.Add(new ClassifiedSpan("", new Microsoft.CodeAnalysis.Text.TextSpan(lastOffset, line.End - lastOffset)));
            //}
            MyShaper.Result res = null;
            foreach (var item in new ClassificationSpan[] { new ClassificationSpan(line.Extent, new DummyClassificationType()) })
            {
                if (!styles.TryGetValue(item.ClassificationType.Classification, out var style))
                    style = defaultStle;
                var oldPoint = point;
                res = style.Shaper.Shape(line.Snapshot.GetText(item.Span), point, style.Paint);
                point = res.EndPoint;
                RenderList.Add(new RenderPart() { Paint = style.Paint, Text = res });
                Height = Math.Max(Height, style.Paint.FontSpacing);
            }
            Width = res?.EndPoint.X ?? 0;
        }

        class DummyClassificationType : IClassificationType
        {
            public string Classification => "dummy";

            public IEnumerable<IClassificationType> BaseTypes { get { yield break; } }

            public bool IsOfType(string type)
            {
                return false;
            }
        }

        public void Render(SKCanvas canvas, SKPoint point)
        {
            Top = point.Y;
            canvas.SetMatrix(SKMatrix.MakeTranslation(point.X, (float)(point.Y + Height)));
            foreach (var item in RenderList)
            {
                canvas.DrawPositionedText(item.Text.Text, item.Text.Points, item.Paint);
            }
        }

        public object IdentityTag => throw new NotImplementedException();

        public ITextSnapshot Snapshot => line.Snapshot;

        public bool IsFirstTextViewLineForSnapshotLine => true;

        public bool IsLastTextViewLineForSnapshotLine => true;

        public double Baseline => Bottom;

        public SnapshotSpan Extent => line.Extent;

        public IMappingSpan ExtentAsMappingSpan => throw new NotImplementedException();

        public SnapshotSpan ExtentIncludingLineBreak => line.ExtentIncludingLineBreak;

        public IMappingSpan ExtentIncludingLineBreakAsMappingSpan => throw new NotImplementedException();

        public SnapshotPoint Start => line.Start;

        public int Length => throw new NotImplementedException();

        public int LengthIncludingLineBreak => throw new NotImplementedException();

        public SnapshotPoint End => line.End;

        public SnapshotPoint EndIncludingLineBreak => line.EndIncludingLineBreak;

        public int LineBreakLength => _lineBreakLength;

        public double Left => 0;

        public double Top { get; private set; }

        public double Height { get; private set; }

        public double TextTop => Top;

        public double TextBottom => TextTop + TextHeight;

        public double TextHeight => Height;

        public double TextLeft => 0;

        public double TextRight => Width;

        public double TextWidth => Width;

        public double Width { get; private set; }

        public double Bottom => Top + Height;

        public double Right => Left + Width;

        public double EndOfLineWidth => VirtualSpaceWidth;

        public double VirtualSpaceWidth => 16;//TODO

        public bool IsValid => true;//TODO

        public LineTransform LineTransform => throw new NotImplementedException();

        public LineTransform DefaultLineTransform => throw new NotImplementedException();

        public VisibilityState VisibilityState => VisibilityState.FullyVisible;

        public double DeltaY => throw new NotImplementedException();

        public TextViewLineChange Change => throw new NotImplementedException();

        public bool ContainsBufferPosition(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public TextBounds? GetAdornmentBounds(object identityTag)
        {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<object> GetAdornmentTags(object providerTag)
        {
            throw new NotImplementedException();
        }

        public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate)
        {
            return this.GetBufferPositionFromXCoordinate(xCoordinate, false);
        }

        public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate, bool textOnly)
        {
            this.ThrowIfInvalid();
            if (double.IsNaN(xCoordinate))
                throw new ArgumentOutOfRangeException("xCoordinate");

            if ((xCoordinate < TextLeft) || (xCoordinate >= Width + TextLeft))
                return null;

            if (xCoordinate >= TextRight)
            {
                //A pick over the end of line character
                return _extentIncludingLineBreak.End - _lineBreakLength;
            }

            if (xCoordinate < Right)
            {
                int bufferPosition = 0;
                foreach (var render in RenderList.SelectMany(r => r.Text.Points))
                {
                    bufferPosition++;
                    if (render.X > xCoordinate)
                        break;
                }
                return Start.Add(bufferPosition - 1);
            }

            //TODO verify this is the appropriate return (rather than _extentIncludingLineBreak.End - _lineBreakLength).
            return null;
        }

        public TextBounds GetCharacterBounds(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition)
        {
            this.ThrowIfInvalid();
            bufferPosition = this.FixBufferPosition(bufferPosition);

            //Use the relaxed version since this is often called for the caret position on the ITextViewLine returned
            //as the caret's ContainingTextViewLine.
            if (!this.RelaxedContainsBufferPosition(bufferPosition))
                throw new ArgumentOutOfRangeException("bufferPosition");
            var points = RenderList.SelectMany(r => r.Text.Points).Skip(bufferPosition - Start).Take(2);
            return new TextBounds(points.FirstOrDefault().X, Top, points.LastOrDefault().X, Height, TextTop, TextHeight);
        }

        public TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition)
        {
            this.ThrowIfInvalid();

            // if the point is in virtual space, then it can't be next to any space negotiating adornments, 
            // so just return its character bounds. If the point is not in virtual space, then use the regular
            // GetExtendedCharacterBounds method for a non-virtual SnapshotPoint
            if (bufferPosition.IsInVirtualSpace)
                return this.GetCharacterBounds(bufferPosition);
            else
                return this.GetExtendedCharacterBounds(bufferPosition.Position);
        }

        public VirtualSnapshotPoint GetInsertionBufferPositionFromXCoordinate(double x)
        {
            this.ThrowIfInvalid();

            int virtualSpaces = 0;
            SnapshotPoint? bufferPosition = this.GetBufferPositionFromXCoordinate(x);

            // If the buffer position is in the interior of the line
            if (bufferPosition.HasValue &&
                bufferPosition.Value.Position < _extentIncludingLineBreak.End.Position - _lineBreakLength)
            {
                TextBounds bounds = this.GetExtendedCharacterBounds(bufferPosition.Value);

                // check to see if the provided x coordinate is closer to the trailing edge of the
                // text element and if so, return the following buffer position since x is closer
                // to following buffer position
                if (bounds.IsRightToLeft == (x < bounds.Left + (bounds.Width * 0.5)))
                {
                    bufferPosition = this.GetTextElementSpan(bufferPosition.Value).End;
                }
            }
            else if (x <= TextLeft)
            {
                // return the start of the line if x is left of text left
                bufferPosition = _extentIncludingLineBreak.Start;
            }
            else
            {
                // This is almost identical to the logic in GetVirtualBufferPositionFromXCoordinate,
                // except that it rounds to the nearest column instead of truncating
                bufferPosition = _extentIncludingLineBreak.End - _lineBreakLength;

                // Only return a position in virtual space if at the physical end of line.
                if (this.IsLastTextViewLineForSnapshotLine && (x > TextRight))
                    virtualSpaces = (int)Math.Round((x - TextRight) / VirtualSpaceWidth);
            }

            return new VirtualSnapshotPoint(bufferPosition.Value, virtualSpaces);
        }

        public Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan)
        {
            throw new NotImplementedException();
        }

        public SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public VirtualSnapshotPoint GetVirtualBufferPositionFromXCoordinate(double xCoordinate)
        {
            throw new NotImplementedException();
        }

        public bool IntersectsBufferSpan(SnapshotSpan bufferSpan)
        {
            throw new NotImplementedException();
        }

        #region Private helpers
        private void ThrowIfInvalid()
        {
            if (!this.IsValid)
                throw new ObjectDisposedException("FormattedLine");
        }
        private SnapshotPoint FixBufferPosition(SnapshotPoint bufferPosition)
        {
            if (bufferPosition.Snapshot != _extentIncludingLineBreak.Snapshot)
                throw new ArgumentException("InvalidSnapshotPoint");

            return bufferPosition;
        }

        //A slightly looser definition of ContainsBufferPosition that allows considers a position at the end of a word wrapped line as contained by the line.
        private bool RelaxedContainsBufferPosition(SnapshotPoint bufferPosition)
        {
            this.ThrowIfInvalid();
            bufferPosition = this.FixBufferPosition(bufferPosition);

            return ((bufferPosition >= _extentIncludingLineBreak.Start) &&
                    ((bufferPosition < _extentIncludingLineBreak.End) ||
                     ((bufferPosition == _extentIncludingLineBreak.End) &&
                      (_lineBreakLength == 0))));
        }
        #endregion
    }
}
