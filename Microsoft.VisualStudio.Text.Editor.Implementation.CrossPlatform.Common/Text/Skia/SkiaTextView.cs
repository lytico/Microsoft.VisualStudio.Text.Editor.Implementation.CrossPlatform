using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

#if __MACOS__
using AppKit;
using CoreGraphics;
#endif
namespace Microsoft.VisualStudio.Text.Editor
{
    partial class SkiaTextView
    {

        ITextBuffer _textBuffer;
        ITextSnapshot _textSnapshot;

        ITextBuffer _visualBuffer;
        ITextSnapshot _visualSnapshot;

        IBufferGraph _bufferGraph;
        ITextViewRoleSet _roles;
        ConnectionManager _connectionManager;

        TextEditorFactoryService _factoryService;
        internal readonly IGuardedOperations GuardedOperations;

        IEditorFormatMap _editorFormatMap;

        private bool _hasInitializeBeenCalled = false;

        IClassifier _classifier;
        ITextAndAdornmentSequencer _sequencer;

        internal Microsoft.VisualStudio.Text.Editor.Implementation.SpaceReservationStack _spaceReservationStack;
        SkiaTextSelection _selection;

        private IEditorOptions _editorOptions;
        SkiaTextCaret _caretElement;
        ViewStack _baseLayer;
        ViewStack _overlayLayer;

        private Action _loadedAction = null;
        private bool _hasBeenLoaded = false;
        private IViewScroller _viewScroller;

        private PropertyCollection _properties = new PropertyCollection();

        [Export]
        [Name(PredefinedAdornmentLayers.Text)]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Caret)]
        private static readonly AdornmentLayerDefinition TextAdornmentLayer = new AdornmentLayerDefinition();

        [Export]
        [Name(PredefinedAdornmentLayers.Selection)]
        [Order(Before = PredefinedAdornmentLayers.Text)]
        private static readonly AdornmentLayerDefinition SelectionAndProvisionalHighlightAdornmentLayer = new AdornmentLayerDefinition();

        [Export]
        [Name(PredefinedAdornmentLayers.Caret)]
        [Order(After = PredefinedAdornmentLayers.Text)]
        private static readonly AdornmentLayerDefinition CaretAdornmentLayer = new AdornmentLayerDefinition();

        public SkiaTextView(ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextEditorFactoryService factoryService, bool initialize = true) : this()
        {
            this._factoryService = factoryService;
            this.TextDataModel = textViewModel.DataModel;
            this.TextViewModel = textViewModel;

            _textBuffer = textViewModel.EditBuffer;
            _visualBuffer = textViewModel.VisualBuffer;

            _textSnapshot = _textBuffer.CurrentSnapshot;
            _visualSnapshot = _visualBuffer.CurrentSnapshot;

            classifiedPaints = new Dictionary<string, TextStyle> {
                [""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["comment"] = new TextStyle(new SKPaint { Color = SKColors.Gray, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["keyword"] = new TextStyle(new SKPaint { Color = SKColors.Blue, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["class name"] = new TextStyle(new SKPaint { Color = SKColors.Yellow, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["identifier"] = new TextStyle(new SKPaint { Color = SKColors.Orange, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["punctuation"] = new TextStyle(new SKPaint { Color = SKColors.AliceBlue, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["operator"] = new TextStyle(new SKPaint { Color = SKColors.AliceBlue, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["number"] = new TextStyle(new SKPaint { Color = SKColors.Purple, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["string"] = new TextStyle(new SKPaint { Color = SKColors.Brown, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                ["interface name"] = new TextStyle(new SKPaint { Color = SKColors.GreenYellow, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
                //[""] = new TextStyle(new SKPaint { Color = SKColors.Black, Typeface = this.Typeface, TextSize = FontSize, LcdRenderText = true, IsAntialias = true, SubpixelText = true }),
            };
            InitializeRendering();

            InitializeITextView();

            _editorOptions = _factoryService.EditorOptionsFactoryService.GetOptions(this);
            _editorOptions.Parent = parentOptions;

            if (initialize)
                Initialize();
            else
                throw new NotImplementedException();
        }

        void Initialize()
        {
            _bufferGraph = _factoryService.BufferGraphFactoryService.CreateBufferGraph(this.TextViewModel.VisualBuffer);

            _editorFormatMap = _factoryService.EditorFormatMapService.GetEditorFormatMap(this);

            _baseLayer = new ViewStack(_factoryService.OrderedViewLayerDefinitions, this);
            _overlayLayer = new ViewStack(_factoryService.OrderedOverlayLayerDefinitions, this, isOverlayLayer: true);

            // Create selection and make sure it's created before the caret as the caret relies on the selection being
            // available in its constructor
            _selection = new SkiaTextSelection(this, _editorFormatMap, _factoryService.GuardedOperations);

            // Create caret
            _caretElement = new SkiaTextCaret(this, _selection,
                                             _factoryService.SmartIndentationService,
                                             _editorFormatMap,
                                             _factoryService.GuardedOperations);
        }

        public bool IsVisible { get; set; }

        Stopwatch sw = Stopwatch.StartNew();

        /// <summary>
        /// Do a action (typically some type of EnsureSpanVisible) that really only makes sense to do after the view has been loaded
        /// and the size of the view has been locked down.
        /// </summary>
        /// <remarks>
        /// <para><paramref name="action"/> is performed immediately and then, if the view has not already been loaded, once again when the view is loaded for the 1st time.</para>
        /// 
        /// <para>Only one action can be queued up. If multiple calls are made to this method then only the <paramref name="action"/> for the last call
        /// is execute when the view is loaded.</para>
        /// </remarks>
        public void DoActionThatShouldOnlyBeDoneAfterViewIsLoaded(Action action)
        {
            action();

            if (!_hasBeenLoaded)
            {
                _loadedAction = action;
            }
        }

        internal void BeginInvokeOnMainThread(Action p)
        {
            p();
        }
    }
}
