//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Text.Editor
{
    class CompositionManager
    {
        static CompositionManager instance;

        public static CompositionManager Instance
        {
            get => instance ?? (instance = new CompositionManager());
        }
        public RuntimeComposition RuntimeComposition { get; }
        public IExportProviderFactory ExportProviderFactory { get; }
        public ExportProvider ExportProvider { get;  }

        static readonly Resolver StandardResolver = Resolver.DefaultInstance;
        static readonly PartDiscovery Discovery = PartDiscovery.Combine(
            new AttributedPartDiscoveryV1(StandardResolver),
            new AttributedPartDiscovery(StandardResolver, true));

        public CompositionManager()
        {
            RuntimeComposition = CreateRuntimeCompositionFromDiscovery();
            ExportProviderFactory = RuntimeComposition.CreateExportProviderFactory();
            ExportProvider = ExportProviderFactory.CreateExportProvider();
            //Console.WriteLine("--- exported contracts:");
            //foreach (var part in RuntimeComposition.Parts)
            //{
            //    Console.WriteLine("Part:" + part.TypeRef.FullName);
            //    foreach (var export in part.Exports)
            //        Console.WriteLine(export.ContractName);
            //}
        }
        readonly static string[] MefAssemblies = {
            "Microsoft.VisualStudio.Composition",
            "Microsoft.VisualStudio.CoreUtility",
            "Microsoft.VisualStudio.Text.Data",
            "Microsoft.VisualStudio.Text.Logic",
            "Microsoft.VisualStudio.Text.UI",
            "Microsoft.VisualStudio.Text.Implementation",
            "Microsoft.VisualStudio.Threading",
            //"Microsoft.CodeAnalysis.EditorFeatures",
            //"Microsoft.CodeAnalysis.EditorFeatures.Text"
        };
        RuntimeComposition CreateRuntimeCompositionFromDiscovery()
        {
            var assemblies = new Assembly[MefAssemblies.Length + 1];
            assemblies[MefAssemblies.Length] = Assembly.GetCallingAssembly();
            var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < MefAssemblies.Length; i++)
            {
                try
                {
                    foreach (var asm in currentAssemblies)
                    {
                        if (asm.GetName().Name == MefAssemblies[i])
                        {
                            assemblies[i] = asm;
                            break;
                        }
                    }

                    if (assemblies[i] == null)
                    {
                        // Console.WriteLine("load from disk:" + MefAssemblies[i]);
                        assemblies[i] = Assembly.Load(MefAssemblies[i]);
                    }
                } catch (Exception e) {
                    LoggingService.LogError("Error while loading mef catalog assembly " + MefAssemblies[i], e);
                }
            }

            var parts = Discovery.CreatePartsAsync(assemblies).Result;

            var catalog = ComposableCatalog.Create(StandardResolver)
                                           .WithCompositionService()
                                           .AddParts(parts);

            var discoveryErrors = catalog.DiscoveredParts.DiscoveryErrors;
            if (!discoveryErrors.IsEmpty)
            {
                foreach (var error in discoveryErrors)
                {
                    LoggingService.LogInfo("MEF discovery error", error);
                }

                // throw new ApplicationException ("MEF discovery errors");
            }

            CompositionConfiguration configuration = CompositionConfiguration.Create(catalog);

            if (!configuration.CompositionErrors.IsEmpty)
            {
                // capture the errors in an array for easier debugging
                var errors = configuration.CompositionErrors.SelectMany(e => e);
                foreach (var error in errors)
                {
                    LoggingService.LogInfo("MEF composition error: " + error.Message);
                }

                // For now while we're still transitioning to VSMEF it's useful to work
                // even if the composition has some errors. TODO: re-enable this.
                //configuration.ThrowOnErrors ();
            }

            return RuntimeComposition.CreateRuntimeComposition(configuration);
        }

        /// <summary>
        /// Returns an instance of type T that is exported by some composition part. The instance is shared (singleton).
        /// </summary>
        public static T GetExportedValue<T>()
        {
            return Instance.ExportProvider.GetExportedValue<T>();
        }

        /// <summary>
        /// Returns all instance of type T that are exported by some composition part. The instances are shared (singletons).
        /// </summary>
        public static IEnumerable<T> GetExportedValues<T>()
        {
            return Instance.ExportProvider.GetExportedValues<T>();
        }
    }
}