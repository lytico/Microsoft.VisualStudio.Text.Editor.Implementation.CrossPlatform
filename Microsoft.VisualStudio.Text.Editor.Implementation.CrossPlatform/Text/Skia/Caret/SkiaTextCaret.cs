using System;
using System.Diagnostics;
using System.Security;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using SkiaSharp;

namespace Microsoft.VisualStudio.Text.Editor
{
    internal class SkiaTextCaret : ITextCaret
    {
        #region Private Members
        internal SKPaint _caretBrush; // Internal for unit testing
        internal SKPaint _regularBrush = new SKPaint() { Color = SKColors.Red, Typeface = SKTypeface.FromFamilyName("Menlo"), TextSize = 24, LcdRenderText = true, IsAntialias = true, SubpixelText = true };
        internal SKPaint _overwriteBrush;
        internal SKPaint _defaultOverwriteBrush;
        internal SKPaint _defaultRegularBrush;
        readonly DispatcherTimer _blinkTimer;
        int _blinkInterval;
        double _newOpacity = 1.0;

        VirtualSnapshotPoint _insertionPoint;
        PositionAffinity _caretAffinity;
        SkiaTextView _wpfTextView;
        SkiaTextSelection _selection;
        GuardedOperations _guardedOperations;
        internal double _preferredXCoordinate;
        double _preferredYOffset;
        double _displayedHeight;
        double _displayedWidth;
        bool _updateNeeded = true;
        bool _isClosed = false;
        bool _emptySelection = false;
        bool _isHidden = false;
        bool _forceVirtualSpace = false;

        //Internal for unit testing
        internal SKRect _bounds;
        internal bool _caretGeometryNeedsToBeUpdated = true;
        internal SKRect _caretGeometry;
        internal SKRect _originalCaretGeometry;
        internal bool _isContainedByView;

        internal ISmartIndentationService _smartIndentationService;

        const double _bidiCaretIndicatorWidth = 2.0;
        const double _bidiIndicatorHeightRatio = 10.0;

        bool _overwriteMode;
        private double Opacity;
        private const double _roundOffBuffer = 0.01;
        #endregion // Private Members

        public const double CaretHorizontalPadding = 2.0;
        public const double HorizontalScrollbarPadding = 200.0;

        /// <summary>
        /// Constructs a new selection Element that is bound to the specified editor canvas
        /// </summary>
        /// <param name="wpfTextView">
        /// The WPF Text View that hosts this caret
        /// </param>
        public SkiaTextCaret(
            SkiaTextView wpfTextView, SkiaTextSelection selection,
                ISmartIndentationService smartIndentationService,
                IEditorFormatMap editorFormatMap,
                //IClassificationFormatMap classificationFormatMap,
                GuardedOperations guardedOperations)
        {
            // Verify
            Debug.Assert(wpfTextView != null);
            _wpfTextView = wpfTextView;
            _selection = selection;
            _guardedOperations = guardedOperations;

            _smartIndentationService = smartIndentationService;

            // Set up initial values
            _caretAffinity = PositionAffinity.Successor;
            _insertionPoint = new VirtualSnapshotPoint(new SnapshotPoint(_wpfTextView.TextSnapshot, 0));

            //// Set the regular caret brush
            //_editorFormatMap = editorFormatMap;

            //// store information related to classifications
            //_classificationFormatMap = classificationFormatMap;

            this.SubscribeEvents();

            this.UpdateDefaultBrushes();
            this.UpdateRegularCaretBrush();
            this.UpdateOverwriteCaretBrush();

            //Set the default values for the caret to be what they should be for a hidden caret that is not in overwrite mode.
            _caretBrush = _regularBrush;

            // Get the caret blink time from the system.  If the caret is set not to flash, the return value
            // will be -1
            _blinkInterval = CaretBlinkTimeManager.GetCaretBlinkTime();
            if (_blinkInterval > 0)
                _blinkTimer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, _blinkInterval), OnTimerElapsed);

            this.UpdateBlinkTimer();
        }


        #region ITextCaret Members

        /// <summary>
        /// Makes the caret visible by scrolling the view up/down/left/right until the caret is visible.
        /// The view is not scrolled at all if the caret is already visible.
        /// </summary>
        public void EnsureVisible()
        {
            _wpfTextView.DoActionThatShouldOnlyBeDoneAfterViewIsLoaded(this.InnerEnsureVisible);
        }

        private void InnerEnsureVisible()
        {
            // If the textview is closed, this should be a no-op
            if (!_wpfTextView.IsClosed)
            {
                ITextViewLine viewLine = this.GetContainingTextViewLine(_insertionPoint.Position, _caretAffinity);
                if (viewLine.VisibilityState != VisibilityState.FullyVisible)
                {
                    SnapshotPoint start = viewLine.Start;
                    ViewRelativePosition? relativePosition = null;

                    if (viewLine.VisibilityState != VisibilityState.Unattached)
                    {
                        //The returned viewLine was one of the lines formatted by the view, so we can rely on its vertical position to decide whether or not
                        //we need to scroll vertically.
                        if (viewLine.Height <= _wpfTextView.ViewportHeight + _roundOffBuffer)
                        {
                            //Make the entire line visible if it is partially hidden.
                            if (viewLine.Top < _wpfTextView.ViewportTop)
                            {
                                relativePosition = ViewRelativePosition.Top;
                            }
                            else //since the line is partially visible or hidden & its top edge is visible, its bottom edge must be off screen ... fix it.
                            {
                                relativePosition = ViewRelativePosition.Bottom;
                            }
                        }
                        else
                        {
                            //The target line is bigger than the view ... make more of it visible if possible.
                            if (viewLine.Bottom < _wpfTextView.ViewportBottom)
                            {
                                //Space at the bottom of the view we can use.
                                relativePosition = ViewRelativePosition.Bottom;
                            }
                            else if (viewLine.Top > _wpfTextView.ViewportTop)
                            {
                                //Space at the top of the view we can use.
                                relativePosition = ViewRelativePosition.Top;
                            }
                        }
                    }
                    else
                    {
                        //The returned view line was formatted by us and is not part of the view.
                        //If the entire line will fit in the view, make it flush with the top or bottom of the screen (depending on
                        //whether or not it was above the top of the view or not).
                        //Reverse the logic if the line it too big for the view (so that if it was off the top of the view, it is made
                        //flush with the bottom of the view and extends off the top, etc.).
                        relativePosition = ((start < _wpfTextView.TextViewLines.FormattedSpan.Start) ==
                                            (viewLine.Height <= _wpfTextView.ViewportHeight + _roundOffBuffer))
                                           ? ViewRelativePosition.Top
                                           : ViewRelativePosition.Bottom;
                    }

                    if (relativePosition.HasValue)
                    {
                        //The desired line is either off the top or bottom of the screen. Do the minimum "scroll" to make it fully visible.
                        _wpfTextView.DisplayTextLineContainingBufferPosition(start, 0.0, relativePosition.Value);
                        viewLine = this.ContainingTextViewLine;
                    }
                }

                Debug.Assert(viewLine != null);

                //Handle horizontal scrolling (with a little padding).
                double amountToScroll = Math.Max(CaretHorizontalPadding,
                                                    Math.Min(HorizontalScrollbarPadding, _wpfTextView.ViewportWidth / 4));

                if (_wpfTextView.ViewportWidth == 0.0)
                {
                    //The view hasn't been loaded yat (or isn't attached to anything). Just leave it left justified.
                    _wpfTextView.ViewportLeft = 0.0;
                }
                else
                {
                    if ((_bounds.Left - CaretHorizontalPadding) < _wpfTextView.ViewportLeft)
                        _wpfTextView.ViewportLeft = (_bounds.Left - amountToScroll);
                    else if ((_bounds.Right + CaretHorizontalPadding) > _wpfTextView.ViewportRight)
                        _wpfTextView.ViewportLeft = (_bounds.Right + amountToScroll) - _wpfTextView.ViewportWidth;
                }
            }
        }

        public CaretPosition MoveToPreferredCoordinates()
        {
            ITextViewLine preferredTextLine = _wpfTextView.TextViewLines.GetTextViewLineContainingYCoordinate(this.PreferredYCoordinate);

            if (preferredTextLine == null)
            {
                //Since there is always a line at the top of the view ... not getting a line implies we want the last visible line.
                preferredTextLine = _wpfTextView.TextViewLines.LastVisibleLine;
            }

            double xCoordinate = this.MapXCoordinate(preferredTextLine, _preferredXCoordinate, false);

            this.InternalMoveCaretToTextViewLine(preferredTextLine, xCoordinate, this.IsVirtualSpaceOrBoxSelectionEnabled, false, false, true);

            return this.Position;
        }

        public CaretPosition MoveTo(ITextViewLine textLine)
        {
            if (textLine == null)
                throw new ArgumentNullException("textLine");

            double xCoordinate = this.MapXCoordinate(textLine, _preferredXCoordinate, false);

            this.InternalMoveCaretToTextViewLine(textLine, xCoordinate, true, false, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate)
        {
            return this.MoveTo(textLine, xCoordinate, true);
        }

        public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition)
        {
            if (textLine == null)
                throw new ArgumentNullException("textLine");
            if (double.IsNaN(xCoordinate))
                throw new ArgumentOutOfRangeException("xCoordinate");

            xCoordinate = this.MapXCoordinate(textLine, xCoordinate, true);

            this.InternalMoveCaretToTextViewLine(textLine, xCoordinate, true, captureHorizontalPosition, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition)
        {
            this.InternalMoveTo(bufferPosition, PositionAffinity.Successor, true, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        {
            this.InternalMoveTo(bufferPosition, caretAffinity, true, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
        {
            this.InternalMoveTo(bufferPosition, caretAffinity, captureHorizontalPosition, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition)
        {
            this.InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), PositionAffinity.Successor, true, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        {
            this.InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, true, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
        {
            this.InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, captureHorizontalPosition, true, true);

            return this.Position;
        }

        /// <summary>
        /// Moves the caret to the next valid CaretPosition
        /// </summary>
        /// <returns>An CaretPosition containing the valid values of the caret after the move has occurred.</returns>
        /// <remarks>This method will take care of surrogate pairs and combining character sequences.</remarks>
        public CaretPosition MoveToNextCaretPosition()
        {
            CaretPosition oldPosition = this.Position;

            ITextViewLine line = _wpfTextView.GetTextViewLineContainingBufferPosition(oldPosition.BufferPosition);
            if (oldPosition.BufferPosition == line.End && line.IsLastTextViewLineForSnapshotLine)
            {
                //At the physical end of a line, either increase virtual space or move to the start of the next line.
                if (this.IsVirtualSpaceOrBoxSelectionEnabled)
                {
                    //Increase virtual spaces by one.
                    return this.MoveTo(new VirtualSnapshotPoint(line.End, oldPosition.VirtualSpaces + 1));
                }
                else if (oldPosition.BufferPosition == _wpfTextView.TextSnapshot.Length)
                    return oldPosition;
                else
                {
                    //Move to the start of the next line (the position at the end of the line break is also the start of the next line).
                    return this.MoveTo(line.EndIncludingLineBreak, PositionAffinity.Successor, true);
                }
            }
            else
            {
                //Not at the end of a line ... just move to the end of the current text element.
                SnapshotSpan textElementSpan = _wpfTextView.GetTextElementSpan(oldPosition.BufferPosition);
                return this.MoveTo(textElementSpan.End, PositionAffinity.Successor, true);
            }
        }

        /// <summary>
        /// Moves the caret to the previous valid CaretPosition
        /// </summary>
        /// <returns>An CaretPosition containing the valid values of the caret after the move has occurred.</returns>
        /// <remarks>This method will take care of surrogate pairs and combining character sequences.</remarks>
        public CaretPosition MoveToPreviousCaretPosition()
        {
            CaretPosition oldPosition = this.Position;

            if (oldPosition.VirtualSpaces > 0)
            {
                ITextSnapshotLine line = _wpfTextView.TextSnapshot.GetLineFromPosition(oldPosition.BufferPosition);
                int newVirtualSpaces = this.IsVirtualSpaceOrBoxSelectionEnabled ? (oldPosition.VirtualSpaces - 1) : 0;

                return this.MoveTo(new VirtualSnapshotPoint(line.End, newVirtualSpaces));
            }
            else if (oldPosition.BufferPosition == 0)
                return oldPosition;
            else
            {
                //Move to the start of the previous text element.
                SnapshotSpan textElementSpan = _wpfTextView.GetTextElementSpan(oldPosition.BufferPosition - 1);
                return this.MoveTo(textElementSpan.Start, PositionAffinity.Successor, true);
            }
        }

        public bool InVirtualSpace
        {
            get
            {
                return _insertionPoint.IsInVirtualSpace;
            }
        }

        /// <summary>
        /// Switch the caret mode to "overwrite" or "insert".
        /// Note that the caret mode does not precisely track whether or not
        /// we are in Overwrite mode - when there is a selection, the caret
        /// mode is _always_ insert mode. Another example would be when the 
        /// caret is at the end of a line.
        /// </summary>
        public bool OverwriteMode
        {
            get { return _overwriteMode; }

            private set
            {
                if (_overwriteMode != value)
                {
                    _overwriteMode = value;
                    this.UpdateCaretBrush();
                }
            }
        }

        /// <summary>
        /// The caret's actual horizontal offset
        /// </summary>
        public double Left
        {
            get
            {
                return _bounds.Left;
            }
        }

        public double Width
        {
            get
            {
                return _bounds.Width;
            }
        }

        public double Right
        {
            get
            {
                return _bounds.Right;
            }
        }

        /// <summary>
        /// The caret's actual vertical offset
        /// </summary>
        public double Top
        {
            get
            {
                if (!_isContainedByView)
                    throw new InvalidOperationException();

                return _bounds.Top;
            }
        }

        public double Height
        {
            get
            {
                if (!_isContainedByView)
                    throw new InvalidOperationException();

                return _bounds.Height;
            }
        }

        public double Bottom
        {
            get
            {
                if (!_isContainedByView)
                    throw new InvalidOperationException();

                return _bounds.Bottom;
            }
        }

        /// <summary>
        /// Gets the caret's current position
        /// </summary>
        public CaretPosition Position
        {
            get
            {
                //In theory the _insertion point is always at the same snapshot at the _wpfTextView but there could be cases
                //where someone is using the position in a classifier that is using the caret position in the classificaiton changed event.
                //In that case return the old insertion point.
                return new CaretPosition(_insertionPoint,
                                         _wpfTextView.BufferGraph.CreateMappingPoint(_insertionPoint.Position, PointTrackingMode.Positive),
                                         _caretAffinity);
            }
        }

        public ITextViewLine ContainingTextViewLine
        {
            get
            {
                CaretPosition position = this.Position;
                return this.GetContainingTextViewLine(position.BufferPosition, position.Affinity);
            }
        }

        public double PreferredYCoordinate
        {
            get
            {
                //In the cases where the caret extends off the top and bottom of the view, capture only the visible bounds.
                return Math.Max(_wpfTextView.ViewportTop, Math.Min(_wpfTextView.ViewportBottom, _preferredYOffset + _wpfTextView.ViewportTop));
            }
        }

        public bool IsHidden
        {
            get
            {
                return _isHidden;
            }
            set
            {
                _isHidden = value;

                if (!_isHidden)
                    this.InvalidateVisual();
            }
        }


        /// <summary>
        /// An event that fires whenever the caret's position has been explicitly changed.  The event doesn't 
        /// fire if the caret was tracking normal text edits.
        /// </summary>
        public event EventHandler<CaretPositionChangedEventArgs> PositionChanged;

        #endregion // ITextCaret Members

        /// <summary>
        /// Renders the selection geometry on the adorner layer
        /// </summary>
        internal void OnRender(SKCanvas drawingContext)
        {
            if (_updateNeeded)
                this.UpdateCaret();

            this.Opacity = _newOpacity;
            if (this.IsShownOnScreen)
            {
                //Only reset the timer if it wasn't running.
                if ((_blinkTimer != null) && !_blinkTimer.IsEnabled)
                {
                    this.UpdateBlinkTimer();
                }
                drawingContext.SetMatrix(SKMatrix.MakeIdentity());
                drawingContext.DrawRect(_caretGeometry, _caretBrush);
            }
            else
            {
                this.DisableBlinkTimer();
            }
        }

        #region Private Helpers

        private bool UpdateDefaultBrushes()
        {
            //TextFormattingRunProperties plainTextProperties = _classificationFormatMap.DefaultTextProperties;

            //// brushes will be frozen when they're used
            //if (plainTextProperties.ForegroundBrushEmpty)
            //{
            //    _defaultRegularBrush = SystemColors.WindowTextBrush;
            //    _defaultOverwriteBrush = SystemColors.WindowTextBrush.Clone();
            //    _defaultOverwriteBrush.Opacity = 0.5;

            //    return true;
            //}
            //else if (!plainTextProperties.ForegroundBrushSame(_defaultRegularBrush))
            //{
            //    _defaultRegularBrush = plainTextProperties.ForegroundBrush;
            //    _defaultOverwriteBrush = _defaultRegularBrush.Clone();
            //    _defaultOverwriteBrush.Opacity = 0.5;

            //    return true;
            //}

            return false;
        }

        private void UpdateRegularCaretBrush()
        {
            //ResourceDictionary resourceDictionary = _editorFormatMap.GetProperties("Caret");
            //if (resourceDictionary.Contains(EditorFormatDefinition.ForegroundColorId))
            //{
            //    Color color = (Color)resourceDictionary[EditorFormatDefinition.ForegroundColorId];

            //    _regularBrush = new SolidColorBrush(color);
            //}
            //else if (resourceDictionary.Contains(EditorFormatDefinition.ForegroundBrushId))
            //{
            //    _regularBrush = (Brush)resourceDictionary[EditorFormatDefinition.ForegroundBrushId];
            //}
            //else
            //{
            //    _regularBrush = _defaultRegularBrush;
            //}

            //if (_regularBrush.CanFreeze)
            //_regularBrush.Freeze();
        }

        private void UpdateOverwriteCaretBrush()
        {
            //ResourceDictionary resourceDictionary = _editorFormatMap.GetProperties("Overwrite Caret");
            //if (resourceDictionary.Contains(EditorFormatDefinition.ForegroundColorId))
            //{
            //    Color color = (Color)resourceDictionary[EditorFormatDefinition.ForegroundColorId];

            //    _overwriteBrush = new SolidColorBrush(color);
            //    _overwriteBrush.Opacity = 0.5;
            //}
            //else if (resourceDictionary.Contains(EditorFormatDefinition.ForegroundBrushId))
            //{
            //    _overwriteBrush = (Brush)resourceDictionary[EditorFormatDefinition.ForegroundBrushId];
            //}
            //else
            //{
            //    _overwriteBrush = _defaultOverwriteBrush;
            //}

            //if (_overwriteBrush.CanFreeze)
            //_overwriteBrush.Freeze();
        }

        //This will format the lines if the buffer is not in the range of formatted lines.
        private ITextViewLine GetContainingTextViewLine(SnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        {
            ITextViewLine textLine = _wpfTextView.GetTextViewLineContainingBufferPosition(bufferPosition);

            if ((caretAffinity == PositionAffinity.Predecessor) && (textLine.Start == bufferPosition) &&
                (_wpfTextView.TextSnapshot.GetLineFromPosition(bufferPosition).Start != bufferPosition))
            {
                //The desired location has precedessor affinity at the start of a word wrapped line, so we
                //really want the line before this one.
                textLine = _wpfTextView.GetTextViewLineContainingBufferPosition(bufferPosition - 1);
            }

            return textLine;
        }

        private void UpdateBlinkTimer()
        {
            this.Opacity = 1.0;

            if (_blinkTimer != null)
            {
                int blinkInterval = CaretBlinkTimeManager.GetCaretBlinkTime();
                if (_wpfTextView.IsVisible && blinkInterval > 0)
                {
                    if (_blinkInterval != blinkInterval)
                    {
                        // Setting this value may cause a context switch.  Only set it if the blink
                        // interval has changed.
                        _blinkTimer.Interval = new TimeSpan(0, 0, 0, 0, blinkInterval);
                        _blinkInterval = blinkInterval;
                    }
                    _blinkTimer.Start();
                }
                else
                {
                    DisableBlinkTimer();
                }
                _newOpacity = 1.0;
            }
        }

        private void DisableBlinkTimer()
        {
            if (_blinkTimer != null)
            {
                _blinkTimer.Stop();
            }
        }

        /// <summary>
        /// Updates the caret brush when the Overwrite and Regular Caret brushes have been updated by user
        /// </summary>
        private void UpdateCaretBrush()
        {
            if (_overwriteMode)
            {
                // Caret is displayed as an overwrite caret
                _caretBrush = _overwriteBrush;
            }
            else
            {
                // Caret is displayed as an insert caret
                _caretBrush = _regularBrush;
            }

            if (this.IsShownOnScreen)
                this.InvalidateVisual();
        }

        private void InternalMoveCaretToTextViewLine(ITextViewLine textLine, double xCoordinate, bool allowPlacementInVirtualSpace, bool captureHorizontalPosition, bool captureVerticalPosition, bool raiseEvent)
        {
            VirtualSnapshotPoint bufferPosition = textLine.GetInsertionBufferPositionFromXCoordinate(xCoordinate);

            // if placement in virtual space is not allowed, then pin the point to the closest
            // real position
            if (!allowPlacementInVirtualSpace)
                bufferPosition = new VirtualSnapshotPoint(bufferPosition.Position);

            //determine caret affinity. In general this is successor affinity unless the caret is at the end of a line that is word wrapped (in which case it is predecessor affinity so
            //it will be placed at the end of the word wrapped line).
            PositionAffinity caretAffinity = ((!textLine.IsLastTextViewLineForSnapshotLine) && bufferPosition.Position == textLine.End) ? PositionAffinity.Predecessor : PositionAffinity.Successor;

            this.InternalMoveCaret(bufferPosition, caretAffinity, textLine, captureHorizontalPosition, captureVerticalPosition, raiseEvent);
        }

        private void InternalMoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition, bool captureVerticalPosition, bool raiseEvent)
        {
            // Make sure the given position is in the text view's currently rendered snapshot
            if (bufferPosition.Position.Snapshot != _wpfTextView.TextSnapshot)
            {
                throw new ArgumentException("Strings.InvalidSnapshotPoint", "bufferPosition");
            }

            ITextViewLine textLine = this.GetContainingTextViewLine(bufferPosition.Position, caretAffinity);

            // ensure that bufferPosition is not in the middle of a text element
            var newBufferPosition = NormalizePosition(bufferPosition, textLine);
            if (newBufferPosition != bufferPosition)
            {
                //The caret really moved (probably because it was inside an outlining region that got collapsed). Raise the appropriate event later on.
                raiseEvent = true;
            }

            this.InternalMoveCaret(newBufferPosition, caretAffinity, textLine, captureHorizontalPosition, captureVerticalPosition, raiseEvent);
        }

        internal static VirtualSnapshotPoint NormalizePosition(VirtualSnapshotPoint bufferPosition, ITextViewLine textLine)
        {
            if (bufferPosition.IsInVirtualSpace
                ? ((!textLine.IsLastTextViewLineForSnapshotLine) || (bufferPosition.Position != textLine.End))
                : (bufferPosition.Position != textLine.Start))
            {
                bufferPosition = new VirtualSnapshotPoint(
                        bufferPosition.Position < textLine.End ?
                        textLine.GetTextElementSpan(bufferPosition.Position).Start :
                        textLine.End);
            }

            return bufferPosition;
        }

        private void InternalMoveCaret(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, ITextViewLine textLine, bool captureHorizontalPosition, bool captureVerticalPosition, bool raiseEvent)
        {
            CaretPosition oldPosition = this.Position;

            _caretAffinity = caretAffinity;
            _insertionPoint = bufferPosition;
            _forceVirtualSpace = _insertionPoint.IsInVirtualSpace && !this.IsVirtualSpaceOrBoxSelectionEnabled;

            _emptySelection = _wpfTextView.Selection.IsEmpty;
            _isContainedByView = (textLine.VisibilityState != VisibilityState.Unattached);

            double xCoordinate;
            double width;

            if (bufferPosition.IsInVirtualSpace || textLine.End == bufferPosition.Position)
            {
                //Never show overwrite caret when at the physical end of a line.
                this.OverwriteMode = false;
            }
            else
            {
                //Position is in the interior of the line ... preferred position is based strictly on the bufferPosition.
                this.OverwriteMode = _wpfTextView.Options.IsOverwriteModeEnabled() && _emptySelection;
            }

            // if we're in overwrite mode, draw a rectangle covering the text element, otherwise, just 
            // draw a thin line
            if (_overwriteMode)
            {
                Microsoft.VisualStudio.Text.Formatting.TextBounds bounds = textLine.GetExtendedCharacterBounds(bufferPosition);
                xCoordinate = bounds.Left;
                width = bounds.Width;
            }
            else
            {
                xCoordinate = GetXCoordinateFromVirtualBufferPosition(textLine, bufferPosition);

                width = 10;//TODO: SystemParameters.CaretWidth;
            }

            _bounds = new SKRect((float)xCoordinate, (float)textLine.TextTop, (float)(xCoordinate + width), (float)textLine.TextBottom);
            CapturePreferredPositions(captureHorizontalPosition, captureVerticalPosition);

            CaretPosition newPosition = this.Position;
            if (newPosition != oldPosition)
            {
                this.UpdateBlinkTimer();

                if (raiseEvent)
                {
                    if (_selection.IsEmpty)
                    {
                        //Empty selections are logically located at the caret position, so force a selection changed event to be raised
                        //before any of the caret position changed events.
                        _selection.RaiseChangedEvent(emptyBefore: true, emptyAfter: true, moved: true);
                    }

                    // Inform this change to interested parties
                    EventHandler<CaretPositionChangedEventArgs> positionChanged = this.PositionChanged;
                    if (positionChanged != null)
                    {
                        _guardedOperations.RaiseEvent<CaretPositionChangedEventArgs>(this, positionChanged, new CaretPositionChangedEventArgs(_wpfTextView, oldPosition, newPosition));
                    }
                }
            }

            this.InvalidateVisual();
            _updateNeeded = true;
        }

        /// <summary>
        /// Get the caret x coordinate for a virtual buffer position.
        /// </summary>
        /// <remarks>
        /// The x coordinate is always on the trailing edge of the previous character,
        /// *unless* the supplied buffer position is the first character on the line or
        /// is in virtual space.
        /// </remarks>
        internal static double GetXCoordinateFromVirtualBufferPosition(ITextViewLine textLine, VirtualSnapshotPoint bufferPosition)
        {
            return (bufferPosition.IsInVirtualSpace || bufferPosition.Position == textLine.Start) ?
                textLine.GetExtendedCharacterBounds(bufferPosition).Leading :
                textLine.GetExtendedCharacterBounds(bufferPosition.Position - 1).Trailing;
        }

        /// <summary>
        /// Constructs Caret Geometry
        /// </summary>
        void ConstructCaretGeometry()
        {
            _displayedWidth = _bounds.Width;
            _displayedHeight = _bounds.Height;
            _caretGeometryNeedsToBeUpdated = false;

            //PathGeometry pathGeometry = new PathGeometry();
            //pathGeometry.AddGeometry(new RectangleGeometry(new Rect(0.0, 0.0, _displayedWidth, _displayedHeight)));

            //TODO: Bidi
            //if (InputLanguageManager.Current.CurrentInputLanguage.TextInfo.IsRightToLeft)
            //{
            //    PathFigure directionGlyph = new PathFigure();
            //    directionGlyph.StartPoint = new Point(0.0, 0.0);
            //    directionGlyph.Segments.Add(new LineSegment(new Point(-_bidiCaretIndicatorWidth, 0), true));
            //    directionGlyph.Segments.Add(new LineSegment(new Point(0, _displayedHeight / _bidiIndicatorHeightRatio), true));
            //    directionGlyph.IsClosed = true;

            //    pathGeometry.Figures.Add(directionGlyph);
            //}

            _caretGeometry = _originalCaretGeometry = new SKRect(0.0f, 0.0f, (float)_displayedWidth, (float)_displayedHeight);
        }

        /// <summary>
        /// Updates the caret based on the current position
        /// </summary>
        internal void UpdateCaret()
        {
            _updateNeeded = false;

            if (_isContainedByView)
            {
                // Caret is on a rendered line.
                if (_caretGeometryNeedsToBeUpdated || (_displayedWidth != _bounds.Width) || _displayedHeight != _bounds.Height)
                {
                    this.ConstructCaretGeometry();
                }

                _caretGeometry = new SKRect(_bounds.Left, _bounds.Top, _bounds.Left + _originalCaretGeometry.Width, _bounds.Top + _originalCaretGeometry.Height);
            }
        }

        void OnFormatMappingChanged(object sender, EventArgs e)
        {
            bool needToUpdate = false;
            //TODO: Caret settings
            //if (e.ChangedItems.Contains("Caret"))
            //{
            //    UpdateRegularCaretBrush();

            //    needToUpdate = true;
            //}

            //if (e.ChangedItems.Contains("Overwrite Caret"))
            //{
            //    UpdateOverwriteCaretBrush();

            //    needToUpdate = true;
            //}

            if (needToUpdate)
                this.UpdateCaretBrush();
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            if (this.UpdateDefaultBrushes())
            {
                this.UpdateRegularCaretBrush();
                this.UpdateOverwriteCaretBrush();
                this.UpdateCaretBrush();
            }
        }

        /// <summary>
        /// Event Handler: Text View Layout Changed Event
        /// </summary>
        internal void LayoutChanged(ITextSnapshot oldSnapshot, ITextSnapshot newSnapshot)
        {
            Debug.Assert(_insertionPoint.Position.Snapshot == oldSnapshot);

            if (oldSnapshot != newSnapshot)
            {
                _insertionPoint = _insertionPoint.TranslateTo(newSnapshot);
            }

            this.UpdateBlinkTimer();

            //This should not cause a caret moved event to be raised but it will update position, bounds & overwrite mode.
            bool textChanges = AnyTextChanges(oldSnapshot.Version, newSnapshot.Version);
            this.RefreshCaret(textChanges);

            if (textChanges && _wpfTextView.Options.IsAutoScrollEnabled())
            {
                if (_insertionPoint.Position.GetContainingLine().LineNumber == newSnapshot.LineCount - 1)
                {
                    _wpfTextView.BeginInvokeOnMainThread(delegate
                    {
                        this.EnsureVisible();
                    });
                }
            }
        }

        private static bool AnyTextChanges(ITextVersion oldVersion, ITextVersion currentVersion)
        {
            while (oldVersion != currentVersion)
            {
                if (oldVersion.Changes.Count > 0)
                    return true;
                oldVersion = oldVersion.Next;
            }

            return false;
        }

        private void RefreshCaret(bool preserveCoordinates, bool clearVirtualSpace = false)
        {
            VirtualSnapshotPoint oldPosition = this.Position.VirtualBufferPosition;

            // If virtual space wasn't forced, we should move the caret out of virtual space
            if (clearVirtualSpace && !_forceVirtualSpace)
            {
                oldPosition = new VirtualSnapshotPoint(oldPosition.Position);
            }

            this.InternalMoveTo(oldPosition, this.Position.Affinity, preserveCoordinates, preserveCoordinates, false);
        }

        /// <summary>
        /// Handle the caret switching from visible to hidden and back. We want to stop listening to
        /// events on other objects when the caret is hidden (so that we won't leak when the view is
        /// destroyed).
        /// </summary>
        void OnVisibleChanged(object sender, EventArgs e)
        {
            //if ((bool)(e.NewValue))
            //{
            //    //TODO: InputLanguageManager.Current.InputLanguageChanged += OnInputLanguageChanged;

            //    this.InvalidateVisual();

            //    //Force the geometry to be recomputed since the input language could have changed when we were not listening
            //    //to the event.
            //    _caretGeometryNeedsToBeUpdated = true;

            //    this.UpdateBlinkTimer();

            //    //note: the update of the win32 caret will happen in OnRender caused by RefreshCaret
            //}
            //else
            //{
            //    //TODO: InputLanguageManager.Current.InputLanguageChanged -= OnInputLanguageChanged;

            //    this.DisableBlinkTimer();
            //}
        }

        void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_wpfTextView.Options.IsOverwriteModeEnabled() && (_wpfTextView.Selection.IsEmpty != _emptySelection))
            {
                this.RefreshCaret(false);
            }
        }

        void OnOptionsChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if (e.OptionId == DefaultTextViewOptions.OverwriteModeId.Name)
            {
                this.RefreshCaret(false);
            }
            else if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceId.Name &&
                     !this.IsVirtualSpaceOrBoxSelectionEnabled)
            {
                this.RefreshCaret(false, true);
            }
        }

        void OnTimerElapsed(object sender, EventArgs e)
        {
            this.InvalidateVisual();
            _newOpacity = (_newOpacity == 0.0) ? 1.0 : 0.0;
        }

        /// <summary>
        /// Rebuild the caret when the input language changes.
        /// </summary>
        //TODO
        //void OnInputLanguageChanged(object sender, InputLanguageEventArgs e)
        //{
        //    this.InvalidateVisual();
        //    _updateNeeded = true;
        //    _caretGeometryNeedsToBeUpdated = true;
        //}

        private void CapturePreferredYCoordinate()
        {
            //Use the bounds if they are meaningful (i.e. the caret is on a line formatted by the view).
            ITextViewLine containingLine = this.ContainingTextViewLine;
            if ((containingLine.VisibilityState == VisibilityState.Unattached) || (containingLine.VisibilityState == VisibilityState.Hidden))
            {
                //Caret is not on screen ... use the first or last visible line instead of the containing line.
                containingLine = _wpfTextView.TextViewLines.LastVisibleLine;
                if (_insertionPoint.Position.Position < containingLine.Start.Position)
                {
                    containingLine = _wpfTextView.TextViewLines.FirstVisibleLine;
                }
            }

            //Since viewportTop is arbitrary, track only the distance from the top of the view to the line.
            _preferredYOffset = (containingLine.Top + containingLine.Height * 0.5) - _wpfTextView.ViewportTop;
        }

        private void CapturePreferredPositions(bool captureHorizontalPosition, bool captureVerticalPosition)
        {
            if (captureHorizontalPosition)
                _preferredXCoordinate = _bounds.Left;

            if (captureVerticalPosition)
                this.CapturePreferredYCoordinate();
        }

        private bool IsVirtualSpaceOrBoxSelectionEnabled
        {
            get
            {
                return _wpfTextView.Options.IsVirtualSpaceEnabled() || _wpfTextView.Selection.Mode == TextSelectionMode.Box;
            }
        }

        /// <summary>
        /// Remaps a given x-coordinate to a valid point. If the provided x-coordinate is past the right end of the line, it will
        /// be clipped to the correct position depending on the virtual space settings. If the ISmartIndent is providing indentation
        /// settings, the x-coordinate will be changed based on that.
        /// </summary>
        private double MapXCoordinate(ITextViewLine textLine, double xCoordinate, bool userSpecifiedXCoordinate)
        {
            // if the clicked point is to the right of the text and virtual space is disabled, the coordinate
            // needs to be fixed
            if ((xCoordinate > textLine.TextRight) && !this.IsVirtualSpaceOrBoxSelectionEnabled)
            {
                double indentationWidth = 0.0;

                // ask the ISmartIndent to see if any indentation is necessary for empty lines
                if (textLine.End == textLine.Start)
                {
                    int? indentation = _smartIndentationService.GetDesiredIndentation(_wpfTextView, textLine.Start.GetContainingLine());
                    if (indentation.HasValue)
                    {
                        //The indentation specified by the smart indent service is desired column position of the caret. Find out how much virtual space
                        //need to be at the end of the line to satisfy that.
                        //TOOD:  _wpfTextView.FormattedLineSource.ColumnWidth instead of 16
                        indentationWidth = Math.Max(0.0, (((double)indentation.Value) * 16 - textLine.TextWidth));

                        // if the coordinate is specified by the user and the user has selected a coordinate to the left
                        // of the indentation suggested by ISmartIndent, overrule the ISmartIndent provided value and
                        // do not use any indentation.
                        if (userSpecifiedXCoordinate && (xCoordinate < (textLine.TextRight + indentationWidth)))
                            indentationWidth = 0.0;
                    }
                }

                xCoordinate = textLine.TextRight + indentationWidth;
            }

            return xCoordinate;
        }

        /// <summary>
        /// Unsubsribes from events the caret is interested in.
        /// </summary>
        private void UnsubscribeEvents()
        {
            //this.IsVisibleChanged -= OnVisibleChanged; TODO: Priority 2

            _wpfTextView.Options.OptionChanged -= OnOptionsChanged;
            _wpfTextView.Selection.SelectionChanged -= OnSelectionChanged;
            //_editorFormatMap.FormatMappingChanged -= OnFormatMappingChanged;
            //_classificationFormatMap.ClassificationFormatMappingChanged -= OnClassificationFormatMappingChanged;

            //if (InputLanguageManager.Current != null)
            //{
            //    InputLanguageManager.Current.InputLanguageChanged -= OnInputLanguageChanged;
            //}
        }

        /// <summary>
        /// Subscribes to events the caret is interested in
        /// </summary>
        private void SubscribeEvents()
        {
            //this.IsVisibleChanged += OnVisibleChanged; TODO: Priority 2
            _wpfTextView.Options.OptionChanged += OnOptionsChanged;
            _wpfTextView.Selection.SelectionChanged += OnSelectionChanged;
            //_editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;
            //_classificationFormatMap.ClassificationFormatMappingChanged += OnClassificationFormatMappingChanged;
        }

        #endregion // Private Helpers

        #region Automation Support

        internal static class AccessibilityConstants
        {
            public const string AutomaionId = "WpfCaret";
            public const string ClassName = "WpfCaret";
            public const string Name = "Caret";
            public const string HelpText = "The caret enables moving and editing of text across all locations of the editor";
        }

        //TODO: a11y
        //public IAccessible AccessibleCaret
        //{
        //    get
        //    {
        //        if (_accessibleCaret == null)
        //            _accessibleCaret = new AccessibleCaret(this, _win32Caret);

        //        return _accessibleCaret;
        //    }
        //}

        #endregion

        public void Close()
        {
            if (!_isClosed)
            {
                _isClosed = true;

                this.UnsubscribeEvents();

                this.DisableBlinkTimer();
            }
        }

        public bool IsShownOnScreen
        {
            get
            {
                //this.Visibility == System.Windows.Visibility.Visible &&
                return _isContainedByView && !_isHidden;
            }
        }

        private void InvalidateVisual()
        {
        }
    }
}
