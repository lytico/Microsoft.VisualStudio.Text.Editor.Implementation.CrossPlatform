using System;
using System.Collections.Generic;
using System.Windows;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using SkiaSharp;

namespace Microsoft.VisualStudio.Text.Editor
{

    internal class ViewStack
    {
        #region Private Members
        internal IList<UIElementData> _elements = new List<UIElementData>();
        Dictionary<string, int> _orderedViewLayerDefinitions;
        bool _isOverlayLayer;
        List<SkiaElement> Children { get; } = new List<SkiaElement>();
        #endregion // Private Members

        public ViewStack(Dictionary<string, int> orderedViewLayerDefinitions, ITextView textView, bool isOverlayLayer = false)
        {
            _orderedViewLayerDefinitions = orderedViewLayerDefinitions;
            _isOverlayLayer = isOverlayLayer;
        }

        public bool TryAddElement(string name, SkiaElement element)
        {
            int rank;
            if (!_orderedViewLayerDefinitions.TryGetValue(name, out rank))
            {
                return false;
            }
                
            UIElementData data = new UIElementData(element, name, rank);

            int position = 0;
            while (position < _elements.Count)
            {
                UIElementData existing = _elements[position];
                if (existing.Rank > rank)
                    break;

                ++position;
            }

            Children.Insert(position, element);

            //var framework = element as FrameworkElement;
            //if (framework != null)
            //{
            //    framework.Width = this.ActualWidth;
            //    framework.Height = this.ActualHeight;
            //}

            _elements.Insert(position, data);

            return true;
        }

        public SkiaElement GetElement(string name)
        {
            foreach (UIElementData data in _elements)
            {
                if (data.Name == name)
                    return data.Element;
            }

            return null;
        }

        public void SetSnapshotAndUpdate(ITextSnapshot snapshot, double deltaX, double deltaY, 
                                         IList<ITextViewLine> newOrReformattedLines, IList<ITextViewLine> translatedLines)
        {
            foreach (UIElementData data in _elements)
            {
                AdornmentLayer layer = data.Element as AdornmentLayer;
                if (layer != null)
                {
                    layer.SetSnapshotAndUpdate(snapshot, deltaX, deltaY, newOrReformattedLines, translatedLines);
                }
            }
        }

        public void SetSize(SKSize size)
        {
            throw new NotImplementedException();
            //this.Width = size.Width;
            //this.Height = size.Height;

            //if (_isOverlayLayer)
            //{
            //    base.ClipToBounds = true;
            //}
            //else
            //{
            //    base.VisualScrollableAreaClip = new Rect(new Point(0.0, 0.0), size);
            //}
        }

        internal class UIElementData
        {
            public readonly SkiaElement Element;
            public readonly string Name;
            public readonly int Rank;

            public UIElementData(SkiaElement element, string name, int rank)
            {
                this.Element = element;
                this.Name = name;
                this.Rank = rank;
            }
        }
    }
}
