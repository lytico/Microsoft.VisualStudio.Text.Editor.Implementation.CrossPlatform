using System;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Editor
{
    partial class SkiaTextView
    {
        public IAdornmentLayer GetAdornmentLayer(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            // we disallow users to access our text or caret adornment layers; if need be, they can easily define
            // their own adornment layers
            if ((name == PredefinedAdornmentLayers.Text) || (name == PredefinedAdornmentLayers.Caret))
                throw new ArgumentOutOfRangeException("name", "The Text and Caret adornment layers cannot be retrieved with this method.");

            bool isOverlayLayer = _factoryService.OrderedOverlayLayerDefinitions.ContainsKey(name);
            var stack = isOverlayLayer ? _overlayLayer : _baseLayer;

            var element = stack.GetElement(name);
            if (element != null)
            {
                //Since we are not one of the excluded names above, we must be an IAdornmentLayer
                return (IAdornmentLayer)element;
            }
            else
            {
                AdornmentLayer layer = new AdornmentLayer(this, name, isOverlayLayer);

                // add the newly created layer to the view stack, this call will correctly place
                // the layer based on its defined order attribute
                if (!stack.TryAddElement(name, layer))
                {
                    throw new ArgumentOutOfRangeException("name", "Could not find a matching AdornmentLayerDefinition export for the requested adornment layer name.");
                }

                return layer;
            }
        }
    }
}
