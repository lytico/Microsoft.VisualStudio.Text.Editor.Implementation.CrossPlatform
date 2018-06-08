using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text.Editor
{
    public class SkiaTextViewLineCollection : List<ITextViewLine>, ITextViewLineCollection
    {
        private ITextView _textView;
        private readonly SnapshotSpan _formattedSpan;
        public SkiaTextViewLineCollection(ITextView textView, IEnumerable<ITextViewLine> collection) : base(collection)
        {
            _textView = textView;
            //Do not include the line break (characters after the line break or between a \r\n really are logically
            //part of the next line).
            _formattedSpan = new SnapshotSpan(_textView.TextSnapshot, Span.FromBounds(this[0].Start,
                                                                                      this[this.Count - 1].EndIncludingLineBreak));
        }

        public ITextViewLine FirstVisibleLine => this[0];

        public ITextViewLine LastVisibleLine => this[this.Count - 1];

        public SnapshotSpan FormattedSpan => _formattedSpan;

        public bool IsValid => true;

        public bool ContainsBufferPosition(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public TextBounds GetCharacterBounds(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public int GetIndexOfTextLine(ITextViewLine textLine)
        {
            throw new NotImplementedException();
        }

        public Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan)
        {
            throw new NotImplementedException();
        }

        public SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
        {
            this.ThrowIfInvalid();
            this.ValidateBufferPosition(bufferPosition);

            int index = this.FindTextViewLineIndexContainingBufferPosition(bufferPosition);
            if (index != -1)
            {
                return this[index];
            }
            else
            {
                // We didn't find any text line that contains the specified buffer position.
                return null;
            }
        }

        public ITextViewLine GetTextViewLineContainingYCoordinate(double y)
        {
            throw new NotImplementedException();
        }

        public Collection<ITextViewLine> GetTextViewLinesIntersectingSpan(SnapshotSpan bufferSpan)
        {
            throw new NotImplementedException();
        }

        public bool IntersectsBufferSpan(SnapshotSpan bufferSpan)
        {
            throw new NotImplementedException();
        }

        private int FindTextViewLineIndexContainingBufferPosition(SnapshotPoint position)
        {
            if (position.Position < this[0].Start.Position)
                return -1;

            {
                //If the position starts on or after the start of the last line, then use the
                //line's ContainsBufferPosition logic to handle the special case at the end of
                //the buffer.
                ITextViewLine lastLine = this[this.Count - 1];

                if (position.Position >= lastLine.Start.Position)
                    return lastLine.ContainsBufferPosition(position) ? (this.Count - 1) : -1;
            }

            int low = 0;
            int high = this.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                ITextViewLine middleLine = this[middle];

                if (position.Position < middleLine.Start.Position)
                    high = middle;
                else
                    low = middle + 1;
            }

            return low - 1;
        }

        private void ValidateBufferPosition(SnapshotPoint bufferPosition)
        {
            if (bufferPosition.Snapshot != _formattedSpan.Snapshot)
                throw new ArgumentException("InvalidSnapshotPoint");
        }

        private void ThrowIfInvalid()
        {
            if (!this.IsValid)
                throw new ObjectDisposedException("TextViewLineCollection");
        }
    }
}
