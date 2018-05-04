using System;
using Gtk;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    partial class SkiaTextView : ITextView
    {

        void InitializeITextView()
        {
            ViewScroller = new SkiaViewScroller(this);
        }

        public bool InLayout => false; //TODO

        public IViewScroller ViewScroller { get; private set; }

        SkiaTextViewLineCollection textViewLines;
        public ITextViewLineCollection TextViewLines { get => textViewLines; }

        public ITextCaret Caret => _caretElement;

        public ITextSelection Selection => _selection;

        public ITrackingSpan ProvisionalTextHighlight { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ITextViewRoleSet Roles => throw new NotImplementedException();

        public ITextBuffer TextBuffer => TextViewModel.EditBuffer;

        public IBufferGraph BufferGraph => _bufferGraph;

        public ITextSnapshot TextSnapshot => TextViewModel.EditBuffer.CurrentSnapshot;

        public ITextSnapshot VisualSnapshot => TextViewModel.VisualBuffer.CurrentSnapshot;

        public ITextViewModel TextViewModel { get; }

        public ITextDataModel TextDataModel { get; }

        public double MaxTextRightCoordinate { get; private set; }

        public double ViewportLeft { get; set; }

        public double ViewportTop { get; private set; }

        public double ViewportRight => ViewportLeft + ViewportWidth;

        public double ViewportBottom => ViewportTop + ViewportHeight;

        public double ViewportWidth { get; private set; }

        public double ViewportHeight { get; private set; }

        public double LineHeight => paint.FontSpacing + 1;

        public bool IsClosed => false;

        public IEditorOptions Options => _editorOptions;

        public bool IsMouseOverViewOrAdornments => throw new NotImplementedException();

        public bool HasAggregateFocus => true;

        public PropertyCollection Properties => _properties;

        public Widget VisualElement => throw new NotImplementedException();

        public TextEditorFactoryService ComponentContext => throw new NotImplementedException();

        public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;
        public event EventHandler ViewportLeftChanged;
        public event EventHandler ViewportHeightChanged;
        public event EventHandler ViewportWidthChanged;
        public event EventHandler<MouseHoverEventArgs> MouseHover;
        public event EventHandler Closed;
        public event EventHandler LostAggregateFocus;
        public event EventHandler GotAggregateFocus;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo)
        {
            throw new NotImplementedException();
        }

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride)
        {
            throw new NotImplementedException();
        }

        public SnapshotSpan GetTextElementSpan(SnapshotPoint point)
        {
            throw new NotImplementedException();
        }

        public ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
        {
            if (textViewLines == null)
                throw new InvalidOperationException("GetTextViewLineContainingBufferPosition called before view is fully initialized.");

            if (IsClosed)
                throw new InvalidOperationException("GetTextViewLineContainingBufferPosition called after the view is closed");

            this.ValidateBufferPosition(bufferPosition);
            return textViewLines.GetTextViewLineContainingBufferPosition(bufferPosition);
        }

        public void QueueSpaceReservationStackRefresh()
        {
            throw new NotImplementedException();
        }

        public ISpaceReservationManager GetSpaceReservationManager(string name)
        {
            throw new NotImplementedException();
        }

        public void QueueAggregateFocusCheck(bool checkForFocus = true)
        {
            throw new NotImplementedException();
        }

        private void ValidateBufferPosition(SnapshotPoint bufferPosition)
        {
            if (bufferPosition.Snapshot != _textSnapshot)
                throw new ArgumentException("InvalidSnapshotPoint");
        }
    }
}
