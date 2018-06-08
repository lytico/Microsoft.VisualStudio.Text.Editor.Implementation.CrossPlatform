using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Utilities;
using SkiaSharp;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Editor
{

	/// <summary>
	/// This class paints a solid colored selection on the view
	/// </summary>
	abstract internal class BrushSelectionPainter
	{
		#region Private Attributes

		/// <summary>
		/// The underlying selection for which painting is performed
		/// </summary>
		protected readonly ITextSelection _textSelection;

		/// <summary>
		/// The adornment layer on which the selection is painted
		/// </summary>
		protected readonly IAdornmentLayer _adornmentLayer;

		internal const double ClipPadding = 1500.0;
		internal const double EdgeWidth = 2.0;
		internal const double Overlap = .5;

		#endregion //Private Attributes

		public BrushSelectionPainter (ITextSelection selection)
		{
			_textSelection = selection;
		}

		#region Static Helper Methods
        public static ISelectionPainter CreatePainter (ITextSelection selection, IAdornmentLayer adornmentLayer, Dictionary<string,object> dictionary, SKColor defaultColor)
		{
			return new SolidColorBrushSelectionPainter (selection, adornmentLayer, new SKPaint () { Color = defaultColor, IsStroke = false });
		}

		internal static void LineTopAndBottom (ITextViewLine line, Span streamSelection, out double lineTop, out double lineBottom)
		{
			// For lineTop, always use TextTop so that lines with line transforms do not draw the selection within the transform
			lineTop = line.TextTop;

			// For lineBottom, always use TextBottom so that lines with line transforms do now draw the selection within the
			// transform.  The 1.0 comes from the default line transform, and prevents spaces from appearing within the line.
			lineBottom = line.TextBottom + 1.0;
		}

		/// <summary>
		/// Calculate the selection rectangles that should be drawn on an ITextViewLine.
		/// </summary>
		/// <param name="line">The line on which the selection rectangles are to be calculated.</param>
		/// <param name="span">The span of the selection on that line.</param>
		/// <param name="selectionEnd">The end of the entire selection.</param>
		/// <param name="isBoxSelection">Is this a box selection?</param>
		/// <param name="isVirtualSpaceEnabled">Is virtual space turned on?</param>
		/// <remarks>internal for testability</remarks>
		internal static IList<Tuple<double, double>> CalculateVisualOverlapsForLine (ITextViewLine line, VirtualSnapshotSpan span, SnapshotPoint selectionEnd, bool isBoxSelection, bool isVirtualSpaceEnabled)
		{
			VirtualSnapshotPoint start = span.Start;
			VirtualSnapshotPoint end = span.End;

			// if the requested start and ending points are the same, then simply return a caret wide bound on the line
			if (start == end) {
				double xLeft = SkiaTextCaret.GetXCoordinateFromVirtualBufferPosition (line, start);
				return new FrugalList<Tuple<double, double>> () { new Tuple<double, double> (xLeft, xLeft + 2) };//TODO: SystemParameters.CaretWidth) };
			}

			//If box selection is on, then every line is the "last" line of the selection.
			bool isLastLineOfSelection = isBoxSelection ||
										 (selectionEnd < line.EndIncludingLineBreak) ||     //Selection ends before the end of line
										 ((line.LineBreakLength == 0) && line.IsLastTextViewLineForSnapshotLine); //Or this is the last line.

			IList<Tuple<double, double>> leftRightPairs;

			//Draw the appropriate bits of the selection for virtual space.
			if (start.Position.Position == line.End.Position) {
				//The start is at (or beyond) the end of the line (therefore nothing inside the line is selected).
				double xStart = SkiaTextCaret.GetXCoordinateFromVirtualBufferPosition (line, start);

				double xEnd;
				if (isLastLineOfSelection) {
					//Selection ends on this line as well ... draw a rectangle between them.
					xEnd = SkiaTextCaret.GetXCoordinateFromVirtualBufferPosition (line, end);
				} else {
					//Selection ends on a subsequent line. Show things differently when virtual space is turned on.
					xEnd = isVirtualSpaceEnabled ? double.MaxValue : (xStart + line.EndOfLineWidth);
				}

				leftRightPairs = new FrugalList<Tuple<double, double>> ();

				if (xEnd > xStart)
					leftRightPairs.Add (new Tuple<double, double> (xStart, xEnd));
			} else {
				//Add the bounds for the text in the line's interior.
				var bounds = line.GetNormalizedTextBounds (new SnapshotSpan (start.Position, end.Position));
				leftRightPairs = new List<Tuple<double, double>> (bounds.Count + 1);
				foreach (var bound in bounds) {
					leftRightPairs.Add (new Tuple<double, double> (bound.Left, bound.Right));
				}

				double xEnd = double.MinValue;
				if (isLastLineOfSelection) {
					if (end.IsInVirtualSpace)
						xEnd = SkiaTextCaret.GetXCoordinateFromVirtualBufferPosition (line, end);
				} else if (isVirtualSpaceEnabled) {
					xEnd = double.MaxValue;
				}

				double xStart = line.TextRight;
				if (xEnd > xStart) {
					if ((leftRightPairs.Count > 0) && (leftRightPairs [leftRightPairs.Count - 1].Item2 >= xStart)) {
						xStart = leftRightPairs [leftRightPairs.Count - 1].Item1;
						leftRightPairs.RemoveAt (leftRightPairs.Count - 1);
					}

					leftRightPairs.Add (new Tuple<double, double> (xStart, xEnd));
				}
			}

			return leftRightPairs;
		}

		/// <summary>
		/// Given a text view and bounds (usually text bounds of a line), adds a rectangle representing the provided bounds to the path argument.
		/// This method will clip the provided bounds if they move beyond the endpoints of the viewport.
		/// </summary>
		internal static void AddRectangleToPath (double left, double top, double right, double bottom, double leftClip, double rightClip, SKPath path)
		{
			left = Math.Max (left, leftClip);
			right = Math.Min (right, rightClip);
			if (right > left) {

				path.AddRect (new SKRect (
					(float)left,
					(float)(top - BrushSelectionPainter.Overlap),
					(float)right,
					(float)(bottom + 2.0 * BrushSelectionPainter.Overlap)));
			}
		}

		#endregion //Static Helper Methods
	}
}
