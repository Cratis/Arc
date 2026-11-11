// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.ProxyGenerator.ControllerBased;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;
using HandlebarsDotNet;

namespace Cratis.Arc.ProxyGenerator;

public static partial class Generator
{
    /// <summary>
    /// Executes a single generation run, one focused step at a time.
    /// </summary>
    /// <param name="settings">The options of the run.</param>
    sealed class GenerationRun(GenerationSettings settings)
    {
        readonly TypesInvolved _typesInvolved = new();
        readonly List<string> _directories = [];
        readonly Dictionary<string, GeneratedFileMetadata> _generatedFiles = [];
        readonly Dictionary<string, (string Content, string SourceTypeName)>? _pendingContent = settings.UseSourceFileAsOutputFile ? [] : null;
        IReadOnlyDictionary<string, string>? _sourceFileMap;
        IReadOnlyDictionary<Type, (IReadOnlyList<string> Commands, IReadOnlyList<string> Queries)>? _descriptorOrigins;

        public async Task<bool> Execute(string assemblyFile)
        {
            assemblyFile = Path.GetFullPath(assemblyFile);
            if (!TryPrepareInput(assemblyFile)) return false;

            var overallStopwatch = Stopwatch.StartNew();
            ConfigureTypeExtensions();
            var phaseStopwatch = Stopwatch.StartNew();
            if (!TypeExtensions.TryInitializeProjectAssemblies(assemblyFile, settings.Message, settings.ErrorMessage)) return false;
            using var projectAssemblies = TypeExtensions.OwnProjectAssemblies();
            ReportPhase("Project assemblies", phaseStopwatch);

            _sourceFileMap = Timed("Source-file map", () => ConfigureSourceFileGrouping(assemblyFile));

            var artifacts = DiscoverArtifacts();
            if (!TryPrepareOutput(artifacts)) return false;

            await WriteArtifacts(artifacts);
            await WriteTypesAndEnums(artifacts.IdentityDetailsTypes);

            await FlushPendingContent();
            var orphanedFiles = RemoveOrphanedFiles();
            UpdateIndexFiles(orphanedFiles);

            CollectProducedFiles(settings.ProducedFiles, _generatedFiles, settings.SkipIndexGeneration, settings.OutputPath);
            settings.Message($"  Overall time: {overallStopwatch.Elapsed}");

            return true;
        }

        static IEnumerable<Type> PublicTypesOf(System.Reflection.Assembly assembly)
        {
            try
            {
                // An open generic definition is skipped: it has no concrete shape to emit, and walking
                // one reaches its own type parameters, which are not types at all.
                return assembly.DefinedTypes.Where(t => !t.IsGenericTypeDefinition && ((t.IsPublic && !t.IsAbstract) || t.IsEnum || t.IsInterface));
            }
            catch
            {
                return [];
            }
        }

        bool TryPrepareInput(string assemblyFile)
        {
            if (!File.Exists(assemblyFile))
            {
                settings.ErrorMessage($"Assembly file '{assemblyFile}' does not exist");
                return false;
            }

            if (!Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            return true;
        }

        void ConfigureTypeExtensions()
        {
            TypeExtensions.SetAssemblyPackageMappings(settings.AssemblyPackageMappings ?? new Dictionary<string, string>());
            TypeExtensions.SetExcludedTypes(settings.ExcludedTypeNames ?? [], settings.ExcludedNamespacePatterns ?? []);
            TypeExtensions.SetNamespaceRoots(settings.NamespaceRoots ?? []);
            TypeExtensions.SetTypeMappings(settings.TypeMappings ?? []);
            GeneratedFileNames.UseProxySuffix(settings.UseProxyFileSuffix);
        }

        IReadOnlyDictionary<string, string>? ConfigureSourceFileGrouping(string assemblyFile)
        {
            var sourceFileMap = settings.UseSourceFileAsOutputFile ? SourceFileResolver.BuildTypeToSourceFileMap(assemblyFile) : null;
            TypeExtensions.SetSourceFileMap(sourceFileMap);

            return sourceFileMap;
        }

        void ReportPhase(string phase, Stopwatch stopwatch) => settings.Message($"  {phase} in {stopwatch.Elapsed}");

        T Timed<T>(string phase, Func<T> action)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = action();
            ReportPhase(phase, stopwatch);
            return result;
        }

        DiscoveredArtifacts DiscoverArtifacts()
        {
            var commands = new List<CommandDescriptor>();
            var queries = new List<QueryDescriptor>();

            Timed("Controller discovery", () =>
            {
                var provider = new ControllerBasedArtifactsProvider(settings.Message, settings.OutputPath, settings.SegmentsToSkip);
                commands.AddRange(provider.Commands);
                queries.AddRange(provider.Queries);
                return provider;
            });

            Timed("Model-bound discovery", () =>
            {
                var provider = new ModelBoundArtifactsProvider(settings.Message, settings.OutputPath, settings.SegmentsToSkip, settings.SkipCommandNameInRoute, settings.SkipQueryNameInRoute, settings.ApiPrefix, settings.EndpointGroup);
                commands.AddRange(provider.Commands);
                queries.AddRange(provider.Queries);
                return provider;
            });

            var identityDetailsTypes = Timed("Identity discovery", () => new IdentityDetailsTypesProvider(settings.Message).IdentityDetailsTypes);

            var isFrameworkNeutral = settings.Framework == ProxyFramework.None;
            var filteredCommands = commands.Where(c => !c.Type.IsExcluded()).Select(c => c with { IsFrameworkNeutral = isFrameworkNeutral }).ToList();
            var filteredQueries = queries.Where(q => !q.Type.IsExcluded()).Select(q => q with { IsFrameworkNeutral = isFrameworkNeutral }).ToList();

            settings.Message($"  Found {filteredCommands.Count} commands and {filteredQueries.Count} queries");

            return new(filteredCommands, filteredQueries, identityDetailsTypes);
        }

        bool TryPrepareOutput(DiscoveredArtifacts artifacts)
        {
            var collisions = ProxyNameCollisions.Find([.. artifacts.Commands, .. artifacts.Queries], settings.SegmentsToSkip, _sourceFileMap);
            if (!TryPrepareOutputDirectory(collisions)) return false;

            _descriptorOrigins = DescriptorExtensions.BuildDescriptorOrigins(artifacts.Commands, artifacts.Queries);
            return true;
        }

        bool TryPrepareOutputDirectory(IReadOnlyList<ProxyNameCollision> collisions)
        {
            if (collisions.Count > 0)
            {
                ProxyNameCollisions.Report(collisions, settings.ErrorMessage);
                return false;
            }

            var outputPath = settings.OutputPath;
            if (Directory.Exists(outputPath) && !settings.SkipOutputDeletion) Directory.Delete(outputPath, true);
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            return true;
        }

        Task Write(IEnumerable<IDescriptor> descriptors, HandlebarsTemplate<object, object> template, string typeNameToEcho) =>
            descriptors.WriteWithFileHeader(settings.FileHeaderTemplate, settings.OutputPath, _typesInvolved, template, _directories, settings.SegmentsToSkip, typeNameToEcho, settings.Message, settings.ErrorMessage, _generatedFiles, _descriptorOrigins, _sourceFileMap, _pendingContent);

        async Task WriteArtifacts(DiscoveredArtifacts artifacts)
        {
            await Write(artifacts.Commands, TemplateTypes.Command, "commands");
            await Write(artifacts.Queries.Where(_ => !_.IsEnumerable && !_.IsObservable).ToList(), TemplateTypes.Query, "single model queries");
            await Write(artifacts.Queries.Where(_ => _.IsEnumerable && !_.IsObservable).ToList(), TemplateTypes.Query, "queries");
            await Write(artifacts.Queries.Where(_ => _.IsObservable).ToList(), TemplateTypes.ObservableQuery, "observable queries");
        }

        async Task WriteTypesAndEnums(IEnumerable<Type> identityDetailsTypes)
        {
            var phaseStopwatch = Stopwatch.StartNew();
            CollectInvolvedTypes(identityDetailsTypes);

            _typesInvolved.RemoveDuplicates();
            var enums = _typesInvolved.Where(_ => _.IsEnum).ToList();
            var enumSet = enums.ToHashSet();

            var typeDescriptors = _typesInvolved.Where(_ => !enumSet.Contains(_) && !_.IsFromMappedAssembly()).ToList().ConvertAll(_ => _.ToTypeDescriptor(settings.OutputPath, settings.SegmentsToSkip));
            ReportPhase("Type collection and descriptors", phaseStopwatch);

            // A class carries `@field` decorators so Fundamentals can reconstruct it from JSON, which means a
            // runtime dependency on `@cratis/fundamentals`. A model that is only ever built and read - never
            // deserialized through that path - does not need either, and a package that deliberately has no
            // dependencies cannot take one on. Interfaces say the same shape and cost nothing.
            var typeTemplate = settings.EmitInterfaces ? TemplateTypes.Interface : TemplateTypes.Type;
            await Write(typeDescriptors, typeTemplate, "types");

            await WriteEnums(enums, TemplateTypes.Enum, "enums", flags: false);
            await WriteEnums(enums, TemplateTypes.FlagsEnum, "flags enums", flags: true);
        }

        Task WriteEnums(List<Type> enums, HandlebarsTemplate<object, object> template, string typeNameToEcho, bool flags)
        {
            var descriptors = enums
                .Where(_ => !_.IsFromMappedAssembly() && _.IsFlagsEnum() == flags)
                .ToList()
                .ConvertAll(_ => _.ToEnumDescriptor());
            return Write(descriptors, template, typeNameToEcho);
        }

        void CollectInvolvedTypes(IEnumerable<Type> identityDetailsTypes)
        {
            foreach (var identityDetailsType in identityDetailsTypes)
            {
                identityDetailsType.CollectTypesInvolved(_typesInvolved);
            }

            if (!settings.LibraryMode) return;

            foreach (var type in TypeExtensions.Assemblies.SelectMany(PublicTypesOf))
            {
                type.CollectTypesInvolved(_typesInvolved);
            }

            settings.Message($"  Library mode: {_typesInvolved.Count} types collected");
        }

        async Task FlushPendingContent()
        {
            // Flush deferred content to disk, comparing final merged hashes against original files
            if (_pendingContent is { Count: > 0 })
            {
                await DescriptorExtensions.FlushPendingContentWithFileHeader(settings.FileHeaderTemplate, _pendingContent, _generatedFiles, settings.Message);
            }
        }

        IEnumerable<string> RemoveOrphanedFiles()
        {
            var stopwatch = Stopwatch.StartNew();
            var orphanedFiles = FileMetadataScanner.FindOrphanedFiles(settings.OutputPath, _generatedFiles);
            var removedCount = FileMetadataScanner.RemoveOrphanedFiles(settings.OutputPath, orphanedFiles, settings.Message);
            if (removedCount > 0)
            {
                settings.Message($"  Removed {removedCount} orphaned file(s) in {stopwatch.Elapsed}");
            }

            ReportPhase("Orphan cleanup", stopwatch);
            return orphanedFiles;
        }

        void UpdateIndexFiles(IEnumerable<string> orphanedFiles)
        {
            if (settings.SkipIndexGeneration)
            {
                settings.Message("  Index file generation skipped");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var distinctDirectories = _directories.Distinct().ToList();
            var (writtenCount, skippedCount) = IndexFileManager.UpdateAllIndexFilesWithFileHeader(settings.FileHeaderTemplate, distinctDirectories, _generatedFiles, orphanedFiles, settings.Message, settings.OutputPath);
            var skippedInfo = skippedCount > 0 ? $" ({skippedCount} unchanged)" : string.Empty;
            settings.Message($"  {writtenCount} index files written{skippedInfo} in {stopwatch.Elapsed}");
        }
    }
}
