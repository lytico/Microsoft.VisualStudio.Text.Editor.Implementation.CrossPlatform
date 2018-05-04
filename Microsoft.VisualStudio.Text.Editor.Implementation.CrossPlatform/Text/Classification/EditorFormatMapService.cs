//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
    using Microsoft.VisualStudio.Text.Utilities;

    [Export(typeof(IEditorFormatMapService))]
    internal sealed class EditorFormatMapService : IEditorFormatMapService
    {
        //[ImportMany]
        //internal List<Lazy<EditorFormatDefinition, IEditorFormatMetadata>> _formats { get; set; }

        //[Import]
        //internal IClassificationFormatMapService _classificationFormatMapService { get; set; }

        //[Import(typeof(IDataStorageService), AllowDefault = true)]
        //internal IDataStorageService _dataStorageService { get; set; }

        [Import]
        GuardedOperations guardedOperations = null;

        private Dictionary<string, IEditorFormatMap> _formatMaps = new Dictionary<string, IEditorFormatMap>();

        public IEditorFormatMap GetEditorFormatMap(string category)
        {
            //IEditorFormatMap formatMap = null;
            //if (!_formatMaps.TryGetValue(category, out formatMap))
            //{
            //    IDataStorage formatMapDataStorage = null;
            //    if (_dataStorageService != null)
            //        formatMapDataStorage = _dataStorageService.GetDataStorage(category);

            //    formatMap = new EditorFormatMap(_formats, formatMapDataStorage, guardedOperations);
            //    _formatMaps[category] = formatMap;
            //}
            //return formatMap;
            return new EditorFormatMap();
        }

        public IEditorFormatMap GetEditorFormatMap(ITextView textView)
        {
            //return textView.Properties.GetOrCreateSingletonProperty(
            //() => new ViewSpecificFormatMap(_classificationFormatMapService, this, textView));
            return new EditorFormatMap();
        }
    }
}
