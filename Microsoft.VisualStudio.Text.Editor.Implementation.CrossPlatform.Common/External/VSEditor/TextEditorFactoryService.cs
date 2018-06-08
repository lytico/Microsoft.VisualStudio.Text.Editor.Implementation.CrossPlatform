//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    using System.ComponentModel;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text.Outlining;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor.Implementation;

    /// <summary>
    /// Provides a VisualStudio Service that aids in creation of Editor Views
    /// </summary>
    [Export(typeof(ITextEditorFactoryService))]
    internal sealed class TextEditorFactoryService : ITextEditorFactoryService, IPartImportsSatisfiedNotification
    {
        [Import]
        internal GuardedOperations GuardedOperations { get; set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import]
        internal IEditorOperationsFactoryService EditorOperationsProvider { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [ImportMany]
        internal List<Lazy<ITextViewModelProvider, IContentTypeAndTextViewRoleMetadata>> TextViewModelProviders { get; set; }

        [Import]
        internal IBufferGraphFactoryService BufferGraphFactoryService { get; set; }

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; }

        [Import]
        internal IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }


        [Import]
        internal ITextSearchService2 TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelectorService { get; set; }


        [ImportMany(typeof(ITextViewCreationListener))]
        internal List<Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> TextViewCreationListeners { get; set; }

        [ImportMany(typeof(ITextViewConnectionListener))]
        internal List<Lazy<ITextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>> TextViewConnectionListeners { get; set; }

        [Import]
        internal ISmartIndentationService SmartIndentationService { get; set; }

        [Import(AllowDefault = true)]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        [Import]
        internal ITextUndoHistoryRegistry UndoHistoryRegistry { get; set; }

        public event EventHandler<TextViewCreatedEventArgs> TextViewCreated;

        private readonly static ITextViewRoleSet _noRoles = new TextViewRoleSet(new string[0]);

        private readonly static ITextViewRoleSet _allRoles = RolesFromParameters(PredefinedTextViewRoles.Analyzable,
                                                                                 PredefinedTextViewRoles.Debuggable,
                                                                                 PredefinedTextViewRoles.Document,
                                                                                 PredefinedTextViewRoles.Editable,
                                                                                 PredefinedTextViewRoles.Interactive,
                                                                                 PredefinedTextViewRoles.Structured,
                                                                                 PredefinedTextViewRoles.Zoomable,
                                                                                 PredefinedTextViewRoles.PrimaryDocument);

        private readonly static ITextViewRoleSet _defaultRoles = RolesFromParameters(PredefinedTextViewRoles.Analyzable,
                                                                                     PredefinedTextViewRoles.Document,
                                                                                     PredefinedTextViewRoles.Editable,
                                                                                     PredefinedTextViewRoles.Interactive,
                                                                                     PredefinedTextViewRoles.Structured,
                                                                                     PredefinedTextViewRoles.Zoomable);

     

        public ITextViewRoleSet NoRoles
        {
            get { return _noRoles; }
        }

        public ITextViewRoleSet AllPredefinedRoles
        {
            get { return _allRoles; }
        }

        public ITextViewRoleSet DefaultRoles
        {
            // notice that Debuggable and PrimaryDocument are excluded!
            get
            {
                return _defaultRoles;
            }
        }

        public ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles)
        {
            return new TextViewRoleSet(roles);
        }

        public ITextViewRoleSet CreateTextViewRoleSet(params string[] roles)
        {
            return new TextViewRoleSet(roles);
        }

        private static ITextViewRoleSet RolesFromParameters(params string[] roles)
        {
            return new TextViewRoleSet(roles);
        }

        [ImportMany]
        private List<Lazy<SpaceReservationManagerDefinition, IOrderable>> _spaceReservationManagerDefinitions = null;
        internal Dictionary<string, int> OrderedSpaceReservationManagerDefinitions = new Dictionary<string, int>();

        public void OnImportsSatisfied()
        {
            // We do this sorting once for all text views 
            IList<Lazy<AdornmentLayerDefinition, IAdornmentLayersMetadata>> orderedLayers = Orderer.Order(_viewLayerDefinitions);
            for (int i = 0; (i < orderedLayers.Count); ++i)
            {
                if (orderedLayers[i].Metadata.IsOverlayLayer)
                {
                    this.OrderedOverlayLayerDefinitions.Add(orderedLayers[i].Metadata.Name, i);
                }
                else
                {
                    this.OrderedViewLayerDefinitions.Add(orderedLayers[i].Metadata.Name, i);
                }
            }

            IList<Lazy<SpaceReservationManagerDefinition, IOrderable>> orderedManagers = Orderer.Order(_spaceReservationManagerDefinitions);
            for (int i = 0; (i < orderedManagers.Count); ++i)
            {
                this.OrderedSpaceReservationManagerDefinitions.Add(orderedManagers[i].Metadata.Name, i);
            }
        }

        [ImportMany]
        private List<Lazy<AdornmentLayerDefinition, IAdornmentLayersMetadata>> _viewLayerDefinitions = null;
        internal Dictionary<string, int> OrderedViewLayerDefinitions = new Dictionary<string, int>();
        internal Dictionary<string, int> OrderedOverlayLayerDefinitions = new Dictionary<string, int>();

        [Import]
        internal IEditorFormatMapService EditorFormatMapService { get; set; }
    }

    public interface IAdornmentLayersMetadata : IOrderable
    {
        [DefaultValue(false)]
        bool IsOverlayLayer { get; }
    }
}
