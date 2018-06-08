using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Editor
{
    class SkiaViewScroller : IViewScroller
    {
        private SkiaTextView textView;

        public SkiaViewScroller(SkiaTextView textView)
        {
            this.textView = textView;
        }

        public void EnsureSpanVisible(SnapshotSpan span)
        {
            throw new NotImplementedException();
        }

        public void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options)
        {
            throw new NotImplementedException();
        }

        public void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options)
        {
            throw new NotImplementedException();
        }

        public void ScrollViewportHorizontallyByPixels(double distanceToScroll)
        {
            throw new NotImplementedException();
        }

        public void ScrollViewportVerticallyByLine(ScrollDirection direction)
        {
            throw new NotImplementedException();
        }

        public void ScrollViewportVerticallyByLines(ScrollDirection direction, int count)
        {
            throw new NotImplementedException();
        }

        public bool ScrollViewportVerticallyByPage(ScrollDirection direction)
        {
            throw new NotImplementedException();
        }

        public void ScrollViewportVerticallyByPixels(double distanceToScroll)
        {
            throw new NotImplementedException();
        }
    }
}
