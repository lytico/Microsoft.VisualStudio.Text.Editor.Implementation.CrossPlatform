using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Utilities;
using SkiaSharp;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Describes the selection element used to render selections
    /// </summary>
    internal class SkiaTextSelection : ITextSelection
    {
        #region Private Members

        bool _isActive;
        bool _activationTracksFocus;
        readonly SkiaTextView _wpfTextView;
        VirtualSnapshotPoint _activePoint;
        VirtualSnapshotPoint _anchorPoint;

        // In box selection mode, these map to the left and right X coordinates (not corrected for affinity)
        // These are meant to be used directly with ITextViewLine.GetInsertionBufferPositionFromXCoordinate
        double _leftX;
        double _rightX;

        internal NormalizedSnapshotSpanCollection _selectedSpans;
        internal List<VirtualSnapshotSpan> _virtualSelectedSpans;

        readonly IEditorFormatMap _editorFormatMap;
        readonly GuardedOperations _guardedOperations;

        TextSelectionMode _selectionMode;

        /// <summary>
        /// Keeps a reference to the adornment layer on which selection is drawn
        /// </summary>
        internal IAdornmentLayer _selectionAdornmentLayer;

        /// <summary>
        /// The painter used to draw the selection on the view
        /// </summary>
        /// <remarks>
        /// internal for testability
        /// </remarks>
        internal ISelectionPainter _focusedPainter;
        internal ISelectionPainter _unfocusedPainter;
        internal ISelectionPainter Painter
        {
            get
            {
                if (_wpfTextView.IsClosed)
                    throw new InvalidOperationException("TextViewClosed");

                return _isActive ? _focusedPainter : _unfocusedPainter;
            }
        }

        /// <summary>
        /// The tagger used to classify non-active lines on a box insertion.
        /// </summary>
        internal SimpleTagger<ClassificationTag> _boxTagger;

        #endregion // Private Members

        /// <summary>
        /// Constructs a new selection Element
        /// </summary>
        public SkiaTextSelection(SkiaTextView wpfTextView, IEditorFormatMap editorFormatMap, GuardedOperations guardedOperations)
        {
            // Verify
            Debug.Assert(wpfTextView != null);
            _wpfTextView = wpfTextView;
            _editorFormatMap = editorFormatMap;
            ActivationTracksFocus = true;

            // Initialize members
            _activePoint = _anchorPoint = new VirtualSnapshotPoint(_wpfTextView.TextSnapshot, 0);

            _selectionMode = TextSelectionMode.Stream;
            _selectionAdornmentLayer = _wpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.Selection);

            _guardedOperations = guardedOperations;

            this.CreateAndSetPainter("Selected Text", ref _focusedPainter, SKColors.Blue.WithAlpha(80));
            this.CreateAndSetPainter("Inactive Selected Text", ref _unfocusedPainter, SKColors.Gray.WithAlpha(80));

            this.Painter.Activate();

            SubscribeToEvents();
        }

        internal void CreateAndSetPainter(string category, ref ISelectionPainter painter, SKColor defaultColor)
        {
            if (painter != null)
            {
                painter.Dispose();
            }

            painter = BrushSelectionPainter.CreatePainter(this, _selectionAdornmentLayer, _editorFormatMap.GetProperties(category), defaultColor);
        }

        private void EnsureVirtualSelectedSpans()
        {
            if (_virtualSelectedSpans == null)
            {
                _virtualSelectedSpans = new List<VirtualSnapshotSpan>();
                if (this.IsEmpty)
                {
                    VirtualSnapshotPoint caretPoint = this.ActivePoint; //== this.AnchorPoint
                    _virtualSelectedSpans.Add(new VirtualSnapshotSpan(caretPoint, caretPoint));
                }
                else
                {
                    if (this.Mode == TextSelectionMode.Box)
                    {
                        SnapshotPoint current = this.Start.Position;
                        VirtualSnapshotPoint end = this.End;

                        do
                        {
                            ITextViewLine line = _wpfTextView.GetTextViewLineContainingBufferPosition(current);

                            VirtualSnapshotSpan? span = this.GetSelectionOnTextViewLine(line);
                            if (span.HasValue)
                                _virtualSelectedSpans.Add(span.Value);

                            if (line.LineBreakLength == 0 && line.IsLastTextViewLineForSnapshotLine)
                                break;      //Just processed last text view line in buffer.

                            current = line.EndIncludingLineBreak;
                        }
                        while ((current.Position <= end.Position.Position) ||                               //Continue while the virtual space version of current
                               (end.IsInVirtualSpace && (current.Position == end.Position.Position)));      //is less than the virtual space position of the end of selection.
                    }
                    else
                    {
                        _virtualSelectedSpans.Add(new VirtualSnapshotSpan(this.Start, this.End));
                    }
                }
            }
        }

        private void EnsureSelectedSpans()
        {
            if (_selectedSpans == null)
            {
                this.EnsureVirtualSelectedSpans();

                if (_virtualSelectedSpans.Count == 1)
                {
                    //This is the most common case so avoid the work below of copying a single span into a new list, etc.
                    _selectedSpans = new NormalizedSnapshotSpanCollection(_virtualSelectedSpans[0].SnapshotSpan);
                }
                else
                {
                    IList<SnapshotSpan> spans = new List<SnapshotSpan>(_virtualSelectedSpans.Count);
                    foreach (var span in _virtualSelectedSpans)
                    {
                        spans.Add(span.SnapshotSpan);
                    }

                    _selectedSpans = new NormalizedSnapshotSpanCollection(spans);
                }
            }
        }

        public bool ActivationTracksFocus
        {
            set
            {
                if (_activationTracksFocus != value)
                {
                    _activationTracksFocus = value;

                    if (_activationTracksFocus)
                        IsActive = _wpfTextView.HasAggregateFocus;
                }
            }
            get
            {
                return _activationTracksFocus;
            }
        }

        public bool IsActive
        {
            set
            {
                if (_isActive != value)
                {
                    if (this.Painter != null)
                        this.Painter.Clear();

                    _isActive = value;

                    if (this.Painter != null)
                        this.Painter.Activate();
                }
            }
            get
            {
                return _isActive;
            }
        }

        public SkiaTextView WpfTextView
        {
            get { return _wpfTextView; }
        }

        #region ITextSelection Members

        public ITextView TextView
        {
            get
            {
                return _wpfTextView;
            }
        }

        public void Select(SnapshotSpan selectionSpan, bool isReversed)
        {
            VirtualSnapshotPoint start = new VirtualSnapshotPoint(selectionSpan.Start);
            VirtualSnapshotPoint end = new VirtualSnapshotPoint(selectionSpan.End);

            if (isReversed)
            {
                this.Select(end, start);
            }
            else
            {
                this.Select(start, end);
            }
        }

        public void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            if (anchorPoint.Position.Snapshot != _wpfTextView.TextSnapshot)
            {
                throw new ArgumentException("InvalidSnapshotSpan", "anchorPoint");
            }
            if (activePoint.Position.Snapshot != _wpfTextView.TextSnapshot)
            {
                throw new ArgumentException("InvalidSnapshotSpan", "activePoint");
            }

            if (anchorPoint == activePoint)
            {
                //For an empty selection, don't worry about text elements (since we expose the caret position
                //which handles the problem for us).
                this.Clear(false);
            }
            else
            {
                var newAnchorPoint = this.NormalizePoint(anchorPoint);
                var newActivePoint = this.NormalizePoint(activePoint);

                if (newAnchorPoint == newActivePoint)
                {
                    this.Clear(false);
                }
                else
                {
                    this.InnerSelect(newAnchorPoint, newActivePoint);
                }
            }
        }

        private void InnerSelect(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            Debug.Assert(anchorPoint != activePoint);
            bool selectionEmptyBeforeChange = this.IsEmpty;

            this.ActivationTracksFocus = true;

            _anchorPoint = anchorPoint;
            _activePoint = activePoint;

            VirtualSnapshotPoint start = _anchorPoint;
            VirtualSnapshotPoint end = _activePoint;
            if (_anchorPoint > _activePoint)
            {
                start = _activePoint;
                end = _anchorPoint;
            }

            if (this.Mode == TextSelectionMode.Box)
            {
                var startLine = _wpfTextView.GetTextViewLineContainingBufferPosition(start.Position);
                var endLine = _wpfTextView.GetTextViewLineContainingBufferPosition(end.Position);

                // Purposefully do not use CaretElement.GetXCoordinateFromVirtualBufferPosition, as that will end
                // up giving coordinates that won't work correctly when we try to get the selection position on a
                // given line with ITextViewLine.GetInsertionBufferPositionFromXCoordinate
                _leftX = startLine.GetExtendedCharacterBounds(start).Leading;
                _rightX = endLine.GetExtendedCharacterBounds(end).Leading;

                if (_rightX < _leftX)
                {
                    double swap = _leftX;
                    _leftX = _rightX;
                    _rightX = swap;
                }
            }

            // refresh selection painting
            this.Painter.Update(true);

            this.RaiseChangedEvent(emptyBefore: selectionEmptyBeforeChange, emptyAfter: this.IsEmpty, moved: true);
        }

        private void Clear(bool resetMode)
        {
            bool selectionEmptyBeforeChange = this.IsEmpty;

            //Move the anchor point to the active point (creating a zero-length selection).
            _anchorPoint = _activePoint;

            this.ActivationTracksFocus = true;

            if (resetMode)
                this.Mode = TextSelectionMode.Stream;

            //clear drawing
            this.Painter.Clear();

            this.RaiseChangedEvent(emptyBefore: selectionEmptyBeforeChange, emptyAfter: true, moved: false);
        }

        public void Clear()
        {
            this.Clear(true);
        }

        public NormalizedSnapshotSpanCollection SelectedSpans
        {
            get
            {
                this.EnsureSelectedSpans();
                return _selectedSpans;
            }
        }

        public ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans
        {
            get
            {
                this.EnsureVirtualSelectedSpans();
                return new ReadOnlyCollection<VirtualSnapshotSpan>(_virtualSelectedSpans);
            }
        }

        public VirtualSnapshotSpan StreamSelectionSpan
        {
            get
            {
                return new VirtualSnapshotSpan(this.Start, this.End);
            }
        }

        public VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line)
        {
            if (line == null)
            {
                throw new ArgumentNullException("line");
            }

            if (line.Snapshot != _wpfTextView.TextSnapshot)
            {
                throw new ArgumentException("The supplied ITextViewLine is on an incorrect snapshot.", "line");
            }

            if (this.IsEmpty)
            {
                VirtualSnapshotPoint caretPoint = this.ActivePoint; //== this.AnchorPoint
                if (line.ContainsBufferPosition(caretPoint.Position))
                    return new VirtualSnapshotSpan(caretPoint, caretPoint);
            }
            else
            {
                VirtualSnapshotPoint start = this.Start;
                VirtualSnapshotPoint end = this.End;
                if ((end.Position.Position >= line.Start) && (start.Position.Position <= line.End))
                {
                    //The line intersects the virtual span of the selection
                    if (this.Mode == TextSelectionMode.Box)
                    {
                        VirtualSnapshotPoint startPoint = line.GetInsertionBufferPositionFromXCoordinate(_leftX);
                        VirtualSnapshotPoint endPoint = line.GetInsertionBufferPositionFromXCoordinate(_rightX);

                        if (startPoint <= endPoint)
                            return new VirtualSnapshotSpan(startPoint, endPoint);
                        else
                            return new VirtualSnapshotSpan(endPoint, startPoint);
                    }
                    else
                    {
                        if (start.Position.Position < line.Start)
                        {
                            //Clip start to the start of the line.
                            start = new VirtualSnapshotPoint(line.Start);
                        }

                        if (end.Position.Position > line.End)
                        {
                            //Clip end to the end of the line
                            end = new VirtualSnapshotPoint(line.EndIncludingLineBreak);
                        }

                        if (start != end)
                            return new VirtualSnapshotSpan(start, end);
                    }
                }
            }
            return null;
        }

        public TextSelectionMode Mode
        {
            get
            {
                return _selectionMode;
            }
            set
            {
                if (_selectionMode != value)
                {
                    _selectionMode = value;

                    if (!this.IsEmpty)
                    {
                        // Re-select the existing anchor->active (we don't need to do this if selection was empty).
                        this.Select(this.AnchorPoint, this.ActivePoint);
                    }
                }
            }
        }

        public bool IsReversed
        {
            get
            {
                return (_activePoint < _anchorPoint);
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (_activePoint == _anchorPoint);
            }
        }

        public VirtualSnapshotPoint ActivePoint
        {
            get { return this.IsEmpty ? _wpfTextView.Caret.Position.VirtualBufferPosition : _activePoint; }
        }

        public VirtualSnapshotPoint AnchorPoint
        {
            get { return this.IsEmpty ? _wpfTextView.Caret.Position.VirtualBufferPosition : _anchorPoint; }
        }

        public VirtualSnapshotPoint Start { get { return this.IsReversed ? this.ActivePoint : this.AnchorPoint; } }

        public VirtualSnapshotPoint End { get { return this.IsReversed ? this.AnchorPoint : this.ActivePoint; } }

        public event EventHandler SelectionChanged;

        #endregion // ITextSelection Members

        #region Private Helpers

        internal void RaiseChangedEvent(bool emptyBefore, bool emptyAfter, bool moved)
        {
            if (moved || (emptyBefore != emptyAfter))
            {
                //Force the selected spans to be recomputed (if needed).
                _virtualSelectedSpans = null;
                _selectedSpans = null;
            }

            if (moved || !(emptyBefore && emptyAfter))
            {
                // Inform listeners of this change
                _guardedOperations.RaiseEvent(this, SelectionChanged);
            }
        }

        private VirtualSnapshotPoint NormalizePoint(VirtualSnapshotPoint point)
        {
            ITextViewLine line = _wpfTextView.GetTextViewLineContainingBufferPosition(point.Position);

            //If point is at the end of the line, return it (including any virtual space offset)
            if (point.Position >= line.End)
            {
                return new VirtualSnapshotPoint(line.End, point.VirtualSpaces);
            }
            else
            {
                //Otherwise align it with the begining of the containing text element &
                //return that (losing any virtual space).
                SnapshotSpan element = line.GetTextElementSpan(point.Position);
                return new VirtualSnapshotPoint(element.Start);
            }
        }

        /// <summary>
        /// Subscribes to interesting events(those that might cause the selection to be redrawn or when the view closes)
        /// </summary>
        private void SubscribeToEvents()
        {
            // Sign up for events that might trigger a redraw of our selection geometries
            _wpfTextView.Options.OptionChanged += OnEditorOptionChanged;

            _editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;

            // When the view is closed unsubscribe from the format map changed event and editor option changed event
            _wpfTextView.Closed += OnViewClosed;
        }

        /// <summary>
        /// Unsubscribes from all event subscriptions
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            _wpfTextView.Options.OptionChanged -= OnEditorOptionChanged;

            _editorFormatMap.FormatMappingChanged -= OnFormatMappingChanged;
            _wpfTextView.Closed -= OnViewClosed;
        }

        /// <summary>
        /// Event Handler: Text View's Layout Changed event
        /// </summary>
        internal void LayoutChanged(bool visualSnapshotChange, ITextSnapshot newEditSnapshot)
        {
            if (this.IsEmpty)
            {
                //For an empty selection, bring the active and anchor points to the new snapshot (so we don't pin the old one)
                //but any old point in the new snapshot will do.
                _activePoint = new VirtualSnapshotPoint(newEditSnapshot, 0);
                _anchorPoint = _activePoint;

                if (visualSnapshotChange)
                {
                    //Make sure the selection spans are on the correct snapshot.
                    _virtualSelectedSpans = null;
                    _selectedSpans = null;
                }
            }
            else if (visualSnapshotChange)
            {
                //Even though the selection may not have changed the snapshots for these are out of date. Delete and let them be lazily recreated.
                var newActivePoint = _activePoint.TranslateTo(newEditSnapshot);
                var newAnchorPoint = _anchorPoint.TranslateTo(newEditSnapshot);

                var normalizedAnchorPoint = this.NormalizePoint(newAnchorPoint);
                var normalizedActivePoint = this.NormalizePoint(newActivePoint);

                if (normalizedActivePoint == normalizedAnchorPoint)
                {
                    //Something happened to collapse the selection (perhaps both endpoints were contained in an outlining region that was collapsed).
                    //Treat this as clearing the selection.
                    this.Clear(false);
                    return;
                }
                else if ((normalizedActivePoint != newActivePoint) || (normalizedAnchorPoint != newAnchorPoint) || (this.Mode == TextSelectionMode.Box))
                {
                    //Something happened to move one endpoint of the selection (outlining region collapsed?).
                    //Treat this as setting the selection (but use InnerSelect since we have the properly normalized endpoints).
                    //
                    //For box selection, we always assume a layout caused something to change (since a classification change could cause the x-coordinate
                    //of one of the endpoints to change, which would change the entire selection). Trying to determine if that is really the case is
                    //expensive (since you need to check each line of the selection). Instead, we pretend it changes so we'll always redraw a box selection
                    //(but the cost there is proportional to the number of visible lines, not selected lines) and hope none of the consumers of the
                    //selection change event do anything expensive.
                    this.InnerSelect(normalizedAnchorPoint, normalizedActivePoint);
                    return;
                }
                else
                {
                    //The selection didn't "change" but the endpoints need to be brought current with the new snapshot.
                    _anchorPoint = normalizedAnchorPoint;
                    _activePoint = normalizedActivePoint;

                    //The cached spans could be affected if there is a visual snapshot change (even if the underlying edit snapshot did not change: c.f. box selection).
                    _virtualSelectedSpans = null;
                    _selectedSpans = null;
                }
            }
            //If the visual snapshot didn't change, it is still possible that a box selection would "change" -- a classifcation change could
            //affect the horizontal position of text on a line -- but we are not going to worry about that case here. A classification change
            //(that would have any effect of selection) without a corresponding change in the buffer is rare enough that we can ignore it.

            this.Painter.Update(false);
        }

        #endregion // Private Helpers

        #region Event Subscriptions

        /// <summary>
        /// Fired when the view is closed.
        /// </summary>
        private void OnViewClosed(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();

            if (_focusedPainter != null)
            {
                _focusedPainter.Dispose();
                _focusedPainter = null;
            }

            if (_unfocusedPainter != null)
            {
                _unfocusedPainter.Dispose();
                _unfocusedPainter = null;
            }
        }

        /// <summary>
        /// Fired when an editor format mapping value is changed
        /// </summary>                  
        void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            //change the selection painters if seleciton settings have changed
            //if (e.ChangedItems.Contains("Selected Text"))
            //{
            //    this.CreateAndSetPainter("Selected Text", ref _focusedPainter, SystemColors.HighlightColor);
            //    if (_isActive)
            //        _focusedPainter.Activate();
            //}
            //if (e.ChangedItems.Contains("Inactive Selected Text"))
            //{
            //    this.CreateAndSetPainter("Inactive Selected Text", ref _unfocusedPainter, SystemColors.GrayTextColor);
            //    if (!_isActive)
            //        _unfocusedPainter.Activate();
            //}
        }

        void OnEditorOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            // If virtual space was just turned off, reselect and remove
            // virtual space
            if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceId.Name)
            {
                if (!(_wpfTextView.Options.IsVirtualSpaceEnabled() || (this.Mode == TextSelectionMode.Box)))
                {
                    VirtualSnapshotPoint newAnchorPoint, newActivePoint;
                    newAnchorPoint = new VirtualSnapshotPoint(AnchorPoint.Position);
                    newActivePoint = new VirtualSnapshotPoint(_wpfTextView.Caret.Position.BufferPosition);

                    // This may send out a change event, which is expected.
                    this.Select(newAnchorPoint, newActivePoint);
                }
            }
            //else if (e.OptionId == DefaultWpfViewOptions.EnableSimpleGraphicsId.Name)
            //{
            //    // update the painter if the option is changed
            //    this.CreateAndSetPainter("Selected Text", ref _focusedPainter, SystemColors.HighlightColor);
            //    this.CreateAndSetPainter("Inactive Selected Text", ref _unfocusedPainter, SystemColors.GrayTextColor);

            //    this.Painter.Activate();
            //}
        }

        #endregion // Event Subscriptions

    }
}
