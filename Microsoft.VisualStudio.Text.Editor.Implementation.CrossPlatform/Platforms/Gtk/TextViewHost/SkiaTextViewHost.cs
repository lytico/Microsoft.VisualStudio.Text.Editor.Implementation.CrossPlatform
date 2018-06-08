//
// SkiaTextViewHost.cs
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
#if __GTK__
using System;
using System.Collections.Generic;
using Gtk;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    public class SkiaTextViewHost : Gtk.VBox
    {
#region Private Members

        SkiaTextView _textView;
        internal TextEditorFactoryService _factory;

        IList<ITextViewMargin> _edges = new List<ITextViewMargin>(5);

        bool _hasInitializeBeenCalled = false;
        bool _readOnly;
        bool _isClosed = false;
        bool _setFocus;
        static Dictionary<string, object> _editorResources;

#endregion

        /// <summary>
        /// Create the WPF text editor Control
        /// </summary>
        public SkiaTextViewHost(bool setFocus, bool initialize = true)
        {
            _factory = (TextEditorFactoryService)PlatformCatalog.Instance.TextEditorFactoryService;
            _setFocus = setFocus;

            if (initialize)
            {
                // Initialize UI
                this.Initialize();
            }
        }

#region IWpfTextViewHost Members

        public bool IsClosed { get { return _isClosed; } }

        public void Close()
        {
            if (_isClosed)
                throw new InvalidOperationException("TextViewHostClosed");

            foreach (var margin in _edges)
            {
                margin.Dispose();
            }
            _textView.Close();

            _isClosed = true;

            _factory.GuardedOperations.RaiseEvent(this, Closed);
        }

        public event EventHandler Closed;

        /// <summary>
        /// Gets the <see cref="IWpfTextViewMargin"/> with the given <paramref name="marginName"/> that is attached to an edge of this <see cref="IWpfTextView"/>
        /// </summary>
        /// <param name="marginName">Name of the <see cref="ITextViewMargin"/>.</param>
        /// <returns>The <see cref="ITextViewMargin"/> with a name that matches <paramref name="marginName"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="marginName"/> is null.</exception>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            if (marginName == null)
                throw new ArgumentNullException("marginName");

            if (!_hasInitializeBeenCalled)
                throw new InvalidOperationException("The margins of the text view host have not been initialized yet. Query margins after the Loaded event is raised");

            foreach (var edge in _edges)
            {
                var result = edge.GetTextViewMargin(marginName) as ITextViewMargin;
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Gets or sets the read-only mode for the text editor
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return _readOnly;
            }

            set
            {
                _readOnly = value;
            }
        }

        /// <summary>
        /// Gets the Text View that is being hosted
        /// </summary>
        internal SkiaTextView TextView
        {
            get
            {
                return _textView;
            }
            set
            {
                _textView = value;
                this.PackStart(_textView, true, true, 0);
            }
        }

#endregion // IWpfTextViewHost Members

#region Private Helpers

        internal bool IsTextViewHostInitialized { get { return _hasInitializeBeenCalled; } }

        // This method should only be called once (it is normally called from the ctor unless we're using
        // ITextEditorFactoryService2.CreateTextViewHostWithoutInitialization on the factory to delay initialization).
        internal void Initialize()
        {
            if (_hasInitializeBeenCalled)
                throw new InvalidOperationException("Attempted to Initialize a WpfTextViewHost twice");

            _hasInitializeBeenCalled = true;

var documentFactoryService = CompositionManager.GetExportedValue<Microsoft.VisualStudio.Text.ITextDocumentFactoryService>();
            var contentTypeRegistryService = CompositionManager.GetExportedValue<Microsoft.VisualStudio.Utilities.IContentTypeRegistryService>();
            var contentType = contentTypeRegistryService.GetContentType("Text");
            var textBuffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer(@"
class Test
{
    public static void Main (string[] args)
    {
        
    }
}            
", contentType);
            var document = documentFactoryService.CreateTextDocument(textBuffer, "/a.cs");
            var dataModel = new VacuousTextDataModel(document.TextBuffer);
            var viewModel = new VacuousTextViewModel(dataModel);
            var textFactory = (TextEditorFactoryService)PlatformCatalog.Instance.TextEditorFactoryService;
            TextView = new SkiaTextView(viewModel, textFactory.AllPredefinedRoles, textFactory.EditorOptionsFactoryService.GlobalOptions, textFactory);
        }

#endregion // Private Handlers
    }
}
#endif