using System;

namespace Microsoft.VisualStudio.Text.Editor
{
	using Microsoft.VisualStudio.Text.Formatting;
	using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.Text;
	using SkiaSharp;
	using Microsoft.VisualStudio.Text.Editor;
	using System.Diagnostics;
	using global::Microsoft.VisualStudio.Text.Editor;

	/// <summary>
	/// This class paints a solid colored selection on the view
	/// </summary>
	sealed internal class SolidColorBrushSelectionPainter : BrushSelectionPainter, ISelectionPainter
	{
		#region Private Attributes

		/// <summary>
		/// Stores a mapping between line identity tags and the virtual snapshot span of the adornment drawn on that line.
		/// </summary>
		internal Dictionary<object, SelectionData> _lineSpans = new Dictionary<object, SelectionData> ();
		VirtualSnapshotSpan _oldStreamSelection;
		private readonly bool _isInContrastMode;
		internal SKPaint Brush { get; private set; }

		#endregion //Private Attributes

		#region Construction

		public SolidColorBrushSelectionPainter (ITextSelection selection, IAdornmentLayer adornmentLayer, SKPaint brush)
			: base (selection)
		{
			this.Brush = brush;
			_isInContrastMode = selection.TextView.Options.GetOptionValue<bool> (DefaultTextViewHostOptions.IsInContrastModeName);
		}

		#endregion //Construction

		#region IDisposable Members
		public void Dispose ()
		{
			if (_lineSpans != null) {
				this.Clear ();
				_lineSpans = null;
			}
		}
		#endregion

		#region ISelectionPainter Members
		public void Clear ()
		{
			_lineSpans.Clear ();
			_oldStreamSelection = new VirtualSnapshotSpan ();   //This will have a null Snapshot but that is good enough here.
		}

		public void Activate ()
		{
			//We can be activated in the ctor (before the view has finished its construction)
			if (!base._textSelection.TextView.InLayout && base._textSelection.TextView.TextViewLines != null)
				this.Update (true);
		}

		public void Update (bool selectionChanged)
		{
			if (base._textSelection.TextView.InLayout) {
				Debug.Assert (false, "Selection can't update its appearance during a layout. Appearance may now be temporarily inconsistent.");
				return;
			}

			VirtualSnapshotSpan streamSelection = base._textSelection.StreamSelectionSpan;
			if (streamSelection.Length == 0) {
				//No selection is easy.
				this.Clear ();
			} else {
				//obtain the lines affected by the selection change (including lines on the old selection).
				IList<ITextViewLine> lines;
				if (_oldStreamSelection.Snapshot != streamSelection.Snapshot) {
					//There has been a text change in the buffer so clear the selection prior to trying to do the update.
					this.Clear ();

					lines = base._textSelection.TextView.TextViewLines.GetTextViewLinesIntersectingSpan (streamSelection.SnapshotSpan);
				} else if (_oldStreamSelection.Length == 0) {
					//Old selection was empty so there should not be any selection.
					Debug.Assert (_lineSpans.Count == 0);
					lines = base._textSelection.TextView.TextViewLines.GetTextViewLinesIntersectingSpan (streamSelection.SnapshotSpan);
				} else {
					IList<ITextViewLine> oldLines = base._textSelection.TextView.TextViewLines.GetTextViewLinesIntersectingSpan (_oldStreamSelection.SnapshotSpan);
					IList<ITextViewLine> newLines = base._textSelection.TextView.TextViewLines.GetTextViewLinesIntersectingSpan (streamSelection.SnapshotSpan);

					//Merge the two lists. They are sorted so do it efficiently.
					lines = new List<ITextViewLine> (oldLines.Count + newLines.Count);
					int iOld = 0;
					int iNew = 0;
					while ((iOld < oldLines.Count) || (iNew < newLines.Count)) {
						ITextViewLine line;
						if (iOld < oldLines.Count) {
							if (iNew < newLines.Count) {
								if (oldLines [iOld].Start.Position < newLines [iNew].Start.Position)
									line = oldLines [iOld++];
								else {
									if (oldLines [iOld].Start.Position == newLines [iNew].Start.Position)
										++iOld;
									line = newLines [iNew++];
								}
							} else
								line = oldLines [iOld++];
						} else
							line = newLines [iNew++];

						lines.Add (line);
					}
				}

				//finally draw the seleciton rectangles
				this.DrawSelectionOnLines (lines, streamSelection.SnapshotSpan);
			}

			_oldStreamSelection = streamSelection;
		}
		#endregion //ISelectionPainter Members

		#region Private Helpers
		private void DrawSelectionOnLines (IList<ITextViewLine> lines, SnapshotSpan streamSelection)
		{
			bool isVirtualSpaceEnabled = base._textSelection.TextView.Options.IsVirtualSpaceEnabled ();
			bool boxSelection = base._textSelection.Mode == TextSelectionMode.Box;

			double viewportLeft = base._textSelection.TextView.ViewportLeft;
			double viewportRight = base._textSelection.TextView.ViewportRight;

			double leftClip = viewportLeft - BrushSelectionPainter.ClipPadding;
			double rightClip = viewportRight + BrushSelectionPainter.ClipPadding;

			SnapshotPoint end = base._textSelection.End.Position;

			//Go through each of the text view lines and see whether the selection on that line has changed. If it hasn't, don't do anything to the adornment for that line.
			foreach (var line in lines) {
				SelectionData oldData;
				bool hadOldSpan = _lineSpans.TryGetValue (line.IdentityTag, out oldData);

				double lineTop;
				double lineBottom;
				BrushSelectionPainter.LineTopAndBottom (line, streamSelection, out lineTop, out lineBottom);

				VirtualSnapshotSpan? newSpan = base._textSelection.GetSelectionOnTextViewLine (line);

				if (hadOldSpan) {
					if (newSpan.HasValue && !oldData.ShouldRedraw (newSpan.Value, viewportLeft, viewportRight, lineTop == line.Top, lineBottom == line.Bottom)) {
						//The span on this line has not changed ... do nothing for this line
						continue;
					}

					//Either there is no new span or the old and new spans disagreed. Remove the data
					//and adorment for the old span.
					base._adornmentLayer.RemoveAdornmentsByTag (line.IdentityTag);
				}

				if (newSpan.HasValue) {
					var bounds = BrushSelectionPainter.CalculateVisualOverlapsForLine (line, newSpan.Value, end, boxSelection, isVirtualSpaceEnabled);
					if (bounds.Count > 0) {
						double leftEdgeOfAdornment = bounds [0].Item1;
						double rightEdgeOfAdornment = bounds [bounds.Count - 1].Item2;

						//Only bother to compute the geometries is there is some overlap between the adornment and the rendering region.
						if ((leftEdgeOfAdornment < rightClip) && (rightEdgeOfAdornment > leftClip)) {
							var path = new SKPath ();
							//path.FillRule = FillRule.Nonzero;
							foreach (var bound in bounds) {
								BrushSelectionPainter.AddRectangleToPath (bound.Item1, lineTop, bound.Item2, lineBottom, leftClip, rightClip, path);
							}

							if (!path.IsEmpty) {
								//TODO: 
								var adornment = new SelectionAdornment (null, this.Brush, path);

								_lineSpans.Add (line.IdentityTag, new SelectionData (newSpan.Value, leftEdgeOfAdornment, rightEdgeOfAdornment, leftClip, rightClip, lineTop == line.Top, lineBottom == line.Bottom));
								_adornmentLayer.AddAdornment (AdornmentPositioningBehavior.TextRelative, line.Extent, line.IdentityTag, adornment, RemovedCallback);
							}
						}
					}
				}
			}
		}
		#endregion //Private Helpers

		public void RemovedCallback (object tag, AdornmentElement element)
		{
			//An adornment was removed ... remove the corresponding entry from the _lineSpans.
			_lineSpans.Remove (tag);
		}

		internal class SelectionData
		{
			public readonly VirtualSnapshotSpan Span;
			public readonly double LeftEdgeOfCorrectlyRenderedAdornment;
			public readonly double RightEdgeOfCorrectlyRenderedAdornment;
			public readonly bool AtTop;                                    //is the adornment's top edge drawn at line.Top.
			public readonly bool AtBottom;                                 // ...                                      Bottom.

			public SelectionData (VirtualSnapshotSpan span, double adornmentLeft, double adornmentRight, double leftClip, double rightClip, bool atTop, bool atBottom)
			{
				this.Span = span;

				//Everything on this adornment between leftClip and rightClip has been rendered correctly (and if the adornment doesn't
				//extend to leftClip then it is rendered "correctly" all the way to double.MinVal & the equivalent for right edge).

				//If the adornment is being clipped, then it is only correctly rendered up to a little bit inside the clip bounds (since there could
				//aliasing artifacts from the edge of the rectangles).
				this.LeftEdgeOfCorrectlyRenderedAdornment = (adornmentLeft < leftClip) ? (leftClip + BrushSelectionPainter.EdgeWidth) : double.MinValue;
				this.RightEdgeOfCorrectlyRenderedAdornment = (adornmentRight > rightClip) ? (rightClip - BrushSelectionPainter.EdgeWidth) : double.MaxValue;

				this.AtTop = atTop;
				this.AtBottom = atBottom;
			}

			public bool ShouldRedraw (VirtualSnapshotSpan span, double viewportLeft, double viewportRight, bool atTop, bool atBottom)
			{
				return (this.Span != span) || (viewportLeft < this.LeftEdgeOfCorrectlyRenderedAdornment) || (viewportRight > this.RightEdgeOfCorrectlyRenderedAdornment) ||
					   (this.AtTop != atTop) || (this.AtBottom != atBottom);
			}
		}
	}
}
