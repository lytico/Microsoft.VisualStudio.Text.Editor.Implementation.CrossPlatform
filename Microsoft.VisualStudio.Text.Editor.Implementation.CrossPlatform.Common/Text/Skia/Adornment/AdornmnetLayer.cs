//
// AdornmnetLayer.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text.Editor
{
    internal class AdornmentLayer : SkiaElement, IAdornmentLayer
    {
        #region Private Members
        SkiaTextView _view;

        //TODO eliminate the duplication between _elements and base.Children (there should be a single master list).
        internal List<AdornmentAndData> _elements;
        string _name;
        bool _isOverlayLayer;
        #endregion // Private Members

        public AdornmentLayer(SkiaTextView view, string name, bool isOverlayLayer = false)
        {
            _view = view;
            _elements = new List<AdornmentAndData>();
            _name = name;

            //An adornment in the overlay layer only supports adornments with the OwnerControlled behavior
            _isOverlayLayer = isOverlayLayer;
        }

        public static bool IsTextRelative(AdornmentPositioningBehavior behavior)
        {
            return (behavior == AdornmentPositioningBehavior.TextRelative) ||
                   (behavior == (AdornmentPositioningBehavior)(AdornmentPositioningBehavior2.TextRelativeVerticalOnly));
        }

        #region IAdornmentLayer Members
        public bool AddAdornment(SnapshotSpan visualSpan, object tag, AdornmentElement element)
        {
            return this.AddAdornment(AdornmentPositioningBehavior.TextRelative, visualSpan, tag, element, null);
        }

        public bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, AdornmentElement element, AdornmentRemovedCallback removedCallback)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (_isOverlayLayer && behavior != AdornmentPositioningBehavior.OwnerControlled)
                throw new ArgumentOutOfRangeException("behavior", "Only AdornmentPositioningBehavior.OwnerControlled is supported");
            if (IsTextRelative(behavior) && !visualSpan.HasValue)
                throw new ArgumentNullException("visualSpan");

            bool visible = true;
            if (visualSpan.HasValue)
            {
                //Does the visual span intersect anything visible?
                visible = _view.TextViewLines.IntersectsBufferSpan(visualSpan.Value);
            }

            if (visible)
            {
                AdornmentAndData data = new AdornmentAndData(behavior, visualSpan, tag, element, removedCallback);
                _elements.Add(data);
                Children.Add(element);
            }

            return visible;
        }

        public void RemoveAllAdornments()
        {
            for (int i = 0; (i < _elements.Count); ++i)
            {
                AdornmentAndData data = _elements[i];

                this.RemoveTranslatableVisual(data);
            }

            _elements = new List<AdornmentAndData>();
        }

        public void RemoveAdornment(AdornmentElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            for (int i = 0; (i < _elements.Count); ++i)
            {
                AdornmentAndData data = _elements[i];
                if (data.Adornment == element)
                {
                    _elements.RemoveAt(i);

                    this.RemoveTranslatableVisual(data);
                    break;
                }
            }
        }

        public void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan)
        {
            InternalRemoveMatchingAdornments(visualSpan, AdornmentLayer.AlwaysTrue);
        }

        public void RemoveAdornmentsByTag(object tag)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");

            InternalRemoveMatchingAdornments(null, (adornment) => object.Equals(adornment.Tag, tag));
        }

        public void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match)
        {
            InternalRemoveMatchingAdornments(null, match);
        }

        public void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match)
        {
            InternalRemoveMatchingAdornments(visualSpan, match);
        }

        public ITextView TextView
        {
            get { return _view; }
        }

        public bool IsEmpty
        {
            get { return _elements.Count == 0; }
        }

        public double Opacity { get; set; }

        public ReadOnlyCollection<IAdornmentLayerElement> Elements
        {
            get
            {
                return new ReadOnlyCollection<IAdornmentLayerElement>(_elements.ConvertAll<IAdornmentLayerElement>(delegate(AdornmentAndData d) { return (IAdornmentLayerElement)d; }));
            }
        }
        #endregion

        // Used by RemoveAdornmentsByVisualSpan, so that we don't need to create a new delegate for each call.
        static bool AlwaysTrue(IAdornmentLayerElement adornment)
        {
            return true;
        }

        private void InternalRemoveMatchingAdornments(SnapshotSpan? visualSpan, Predicate<IAdornmentLayerElement> match)
        {
            List<AdornmentAndData> newVisuals = new List<AdornmentAndData>(_elements.Count);

            for (int i = 0; (i < _elements.Count); ++i)
            {
                AdornmentAndData data = _elements[i];

                if ((!visualSpan.HasValue || data.OverlapsWith(visualSpan.Value)) && match(data))
                {
                    this.RemoveTranslatableVisual(data);
                }
                else
                {
                    newVisuals.Add(data);
                }
            }

            _elements = newVisuals;
        }

        internal void SetSnapshotAndUpdate(ITextSnapshot snapshot, double deltaX, double deltaY,
                                           IList<ITextViewLine> newOrReformattedLines, IList<ITextViewLine> translatedLines)
        {
            //Go through all the added visuals and invalidate or transform as appropriate.
            List<AdornmentAndData> newVisuals = new List<AdornmentAndData>(_elements.Count);

            for (int i = 0; (i < _elements.Count); ++i)
            {
                AdornmentAndData data = _elements[i];

                if (!data.VisualSpan.HasValue)
                {
                    newVisuals.Add(data);
                    if (data.Behavior == AdornmentPositioningBehavior.ViewportRelative)
                    {
                        data.Translate(deltaX, deltaY);
                    }
                }
                else
                {
                    data.SetSnapshot(snapshot);

                    SnapshotSpan span = data.VisualSpan.Value;

                    if ((!_view.TextViewLines.IntersectsBufferSpan(span)) ||
                        (GetCrossingLine(newOrReformattedLines, span) != null))
                    {
                        //Either visual is no longer visible or it crosses a line
                        //that was reformatted.
                        this.RemoveTranslatableVisual(data);
                    }
                    else
                    {
                        newVisuals.Add(data);

                        switch (data.Behavior)
                        {
                            case AdornmentPositioningBehavior.TextRelative:
                            case (AdornmentPositioningBehavior)(AdornmentPositioningBehavior2.TextRelativeVerticalOnly):
                            {
                                ITextViewLine line = GetCrossingLine(translatedLines, span);
                                if (line != null)
                                {
                                    data.Translate((data.Behavior == AdornmentPositioningBehavior.TextRelative)
                                                   ? 0.0
                                                   : deltaX, line.DeltaY);
                                }
                                else if (data.Behavior == (AdornmentPositioningBehavior)(AdornmentPositioningBehavior2.TextRelativeVerticalOnly))
                                {
                                    data.Translate(deltaX, 0.0);
                                }

                                break;
                            }
                            case AdornmentPositioningBehavior.ViewportRelative:
                            {
                                data.Translate(deltaX, deltaY);
                                break;
                            }
                        }
                    }
                }
            }

            _elements = newVisuals;
        }

        //Remove the visual child and call the removed callback. Do not attempt to remove the data from the list of visuals.
        private void RemoveTranslatableVisual(AdornmentAndData data)
        {
            Children.Remove(data.Adornment);

            if (data.RemovedCallback != null)
                data.RemovedCallback(data.Tag, data.Adornment);
        }

        /// <summary>
        /// Find a line in <paramref name="lines"/> that intersects <paramref name="span"/> (if any).
        /// </summary>
        /// <remarks>
        /// <para>A line crosses span if its start is less that the line's end and its end is greater than
        /// or equal to the line's start.</para>
        /// 
        /// <para>In practical terms, an adjoining span to the left of the line crosses and an adjoining span
        /// to the right of the line does not.</para>
        /// </remarks>
        internal static ITextViewLine GetCrossingLine(IList<ITextViewLine> lines, SnapshotSpan span)
        {
            if (lines.Count > 0)
            {
                //Find the line that overlaps the specified span.
                //If span has a length of zero, pretend it has a length of 1.
                int start = span.Start;
                int end = start + Math.Max(1, span.Length);

                int low = 0;
                int high = lines.Count;
                while (low < high)
                {
                    int middle = (low + high) / 2;
                    ITextViewLine middleLine = lines[middle];
                    if (end <= middleLine.Start)
                        high = middle;
                    else if (start >= middleLine.EndIncludingLineBreak)
                        low = middle + 1;
                    else
                        return middleLine;
                }

                //Handle the special case for a zero-length span at the end of the buffer
                //(the only way to have end > the snapshot length was if it was a zero
                //length span at the end of the buffer). A span that starts at the end
                //of the buffer is considered part of the last line in the buffer.
                if (end > span.Snapshot.Length)
                {
                    ITextViewLine lastLine = lines[lines.Count - 1];
                    if (lastLine.End == start)
                        return lastLine;
                }
            }

            return null;
        }

        internal class AdornmentAndData : IAdornmentLayerElement
        {
            private SnapshotSpan? _visualSpan;

            private readonly AdornmentPositioningBehavior _behavior;
            internal readonly AdornmentElement _element;
            private readonly object _tag;
            private readonly AdornmentRemovedCallback _removedCallback;

            public AdornmentAndData(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, AdornmentElement element, AdornmentRemovedCallback removedCallback)
            {
                _behavior = behavior;
                _visualSpan = visualSpan;
                _tag = tag;
                _element = element;
                _removedCallback = removedCallback;
            }

            #region IAdornmentLayerElement Members

            public AdornmentPositioningBehavior Behavior { get { return _behavior; } }

            public AdornmentElement Adornment { get { return _element; } }

            public AdornmentRemovedCallback RemovedCallback { get { return _removedCallback; } }

            public object Tag { get { return _tag; } }

            public SnapshotSpan? VisualSpan { get { return _visualSpan; } }
            #endregion

            public bool OverlapsWith(SnapshotSpan visualSpan)
            {
                if (!_visualSpan.HasValue)
                    return false;

                //The adornment span -- if length 0 -- is treated as if it has
                //a length of 1.
                Span adornmentSpan = new Span(_visualSpan.Value.Start,
                                              Math.Max(1, _visualSpan.Value.Length));

                //We'll consider this an overlap if there is a regular Span overlap with visualSpan or, if visual span
                //contains the end of the buffer, this adornment's visual span starts at the end of the buffer.
                if ((visualSpan.Start < adornmentSpan.End) &&
                    ((visualSpan.End > adornmentSpan.Start) ||
                     ((visualSpan.End == adornmentSpan.Start) &&
                      (adornmentSpan.Start == visualSpan.Snapshot.Length))))
                {
                    return true;
                }

                return false;
            }

            public void SetSnapshot(ITextSnapshot snapshot)
            {
                _visualSpan = _visualSpan.Value.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive);
            }

            public void Translate(double deltaX, double deltaY)
            {
                throw new NotImplementedException();
                //if ((deltaX != 0.0) || (deltaY != 0.0))
                //{
                //    double x = Canvas.GetLeft(_element);
                //    if (double.IsNaN(x))
                //        x = 0.0;

                //    double y = Canvas.GetTop(_element);
                //    if (double.IsNaN(y))
                //        y = 0.0;

                //    Canvas.SetLeft(_element, x + deltaX);
                //    Canvas.SetTop(_element, y + deltaY);
                //}
            }
        }
    }
}
