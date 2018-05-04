//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;

    [Export]
    public class PlatformCatalog
    {
        static PlatformCatalog instance;
        public static PlatformCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CompositionManager.GetExportedValue<PlatformCatalog>();
                }
                return instance;
            }
        }

        [Import]
        internal ITextEditorFactoryService TextEditorFactoryService { get; private set; }
    }
}