//
// SelectionAdornment.cs
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
using SkiaSharp;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Editor
{
	/// <summary>
	/// Acts as a medium to peform the drawing of a selection Geometry path and result in a UIElement
	/// so that it can be used in our adornment system.
	/// </summary>
	public class SelectionAdornment : AdornmentElement
	{
		readonly SKPaint borderPen;
		readonly SKPaint fillBrush;
		readonly SKPath drawingPath;

		public SelectionAdornment (SKPaint borderPen, SKPaint fillBrush, SKPath drawingPath)
		{
			this.drawingPath = drawingPath;
			this.fillBrush = fillBrush;
			this.borderPen = borderPen;
		}

		protected override void Render (SKCanvas canvas)
		{
			canvas.DrawPath (drawingPath, borderPen);
			canvas.DrawPath (drawingPath, fillBrush);
		}
	}
}
