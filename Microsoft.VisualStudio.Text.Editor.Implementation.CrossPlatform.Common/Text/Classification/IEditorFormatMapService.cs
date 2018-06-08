//
// IEditorFormatMapService.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
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
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Looks up a format map for a given view role.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IEditorFormatMapService formatMap = null;
    /// </remarks>
    public interface IEditorFormatMapService
    {
        /// <summary>
        /// Gets an <see cref="IEditorFormatMap"/> appropriate for a given text view. This object is likely
        /// to be shared among several text views.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>An <see cref="IEditorFormatMap"/> for the text view.</returns>
        IEditorFormatMap GetEditorFormatMap(ITextView view);

        /// <summary>
        /// Get a <see cref="IEditorFormatMap"/> for a given appearance category.
        /// </summary>
        /// <param name="category">The appearance category.</param>
        /// <returns>An <see cref="IEditorFormatMap"/> for the category.</returns>
        IEditorFormatMap GetEditorFormatMap(string category);
    }
}
