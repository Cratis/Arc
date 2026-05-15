// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.ProxyGenerator.ControllerBased;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Represents the actual generator.
/// </summary>
public static class Generator
{
    /// <summary>
    /// Generate the proxies from an input assembly and output path.
    /// </summary>
    /// <param name="assemblyFile">Path to the assembly file.</param>
    /// <param name="outputPath">The output path to output to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="errorMessage">Logger to use for outputting error messages.</param>
    /// <param name="libraryMode">When <see langword="true"/>, generates TypeScript proxies for every public type in the assembly, not just commands and queries.</param>
    /// <param name="skipOutputDeletion">True if the output path should be deleted before generating, false if not.</param>
    /// <param name="skipCommandNameInRoute">True if the command name should be skipped in the route, false if not.</param>
    /// <param name="skipQueryNameInRoute">True if the query name should be skipped in the route, false if not.</param>
    /// <param name="apiPrefix">The API prefix to use in the route.</param>
    /// <param name="skipIndexGeneration">True to skip index.ts file generation, false to enable it.</param>
    /// <param name="useSourceFileAsOutputFile">True to group all TypeScript types from the same C# source file into a single .ts file named after the source file, false to generate one file per type.</param>
    /// <param name="assemblyPackageMappings">Optional dictionary mapping assembly names to TypeScript package names. Types from these assemblies will be imported from the package instead of being generated locally.</param>
    /// <param name="excludedTypeNames">Fully qualified type names that should be excluded from proxy generation.</param>
    /// <param name="excludedNamespacePatterns">Namespace glob patterns (supports <c>*</c> as wildcard) whose matching types are excluded from generation.</param>
    /// <param name="namespaceRoots">Pairs of (namespace, base folder) used as roots. When a type's namespace begins with a root the root is stripped and the remainder is placed under the base folder.</param>
    /// <returns>True if successful, false if not.</returns>
    public static async Task<bool> Generate(
        string assemblyFile,
        string outputPath,
        int segmentsToSkip,
        Action<string> message,
        Action<string> errorMessage,
        bool libraryMode = false,
        bool skipOutputDeletion = true,
        bool skipCommandNameInRoute = false,
        bool skipQueryNameInRoute = false,
        string apiPrefix = "api",
        bool skipIndexGeneration = false,
        bool useSourceFileAsOutputFile = false,
        IReadOnlyDictionary<string, string>? assemblyPackageMappings = null,
        IReadOnlyCollection<string>? excludedTypeNames = null,
        IReadOnlyCollection<string>? excludedNamespacePatterns = null,
        IReadOnlyCollection<(string Namespace, string Folder)>? namespaceRoots = null)
    {
        assemblyFile = Path.GetFullPath(assemblyFile);
        if (!File.Exists(assemblyFile))
        {
            errorMessage($"Assembly file '{assemblyFile}' does not exist");
            return false;
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        var overallStopwatch = Stopwatch.StartNew();

        TypeExtensions.SetAssemblyPackageMappings(assemblyPackageMappings ?? new Dictionary<string, string>());
        TypeExtensions.SetExcludedTypes(excludedTypeNames ?? [], excludedNamespacePatterns ?? []);
        TypeExtensions.SetNamespaceRoots(namespaceRoots ?? []);
        TypeExtensions.InitializeProjectAssemblies(assemblyFile, message, errorMessage);

        var commands = new List<CommandDescriptor>();
        var queries = new List<QueryDescriptor>();

        var controllerBasedArtifactsProvider = new ControllerBasedArtifactsProvider(message, outputPath, segmentsToSkip);
        commands.AddRange(controllerBasedArtifactsProvider.Commands);
        queries.AddRange(controllerBasedArtifactsProvider.Queries);

        var modelBoundArtifactsProvider = new ModelBoundArtifactsProvider(message, outputPath, segmentsToSkip, skipCommandNameInRoute, skipQueryNameInRoute, apiPrefix);
        commands.AddRange(modelBoundArtifactsProvider.Commands);
        queries.AddRange(modelBoundArtifactsProvider.Queries);

        var identityDetailsTypesProvider = new IdentityDetailsTypesProvider(message);

        commands = commands.Where(c => !c.Type.IsExcluded()).ToList();
        queries = queries.Where(q => !q.Type.IsExcluded()).ToList();

        message($"  Found {commands.Count} commands and {queries.Count} queries");

        if (Directory.Exists(outputPath) && !skipOutputDeletion) Directory.Delete(outputPath, true);
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        var typesInvolved = new List<Type>();
        var directories = new List<string>();
        var generatedFiles = new Dictionary<string, GeneratedFileMetadata>();
        var descriptorOrigins = DescriptorExtensions.BuildDescriptorOrigins(commands, queries);

        IReadOnlyDictionary<string, string>? sourceFileMap = null;
        Dictionary<string, (string Content, string SourceTypeName)>? pendingContent = null;
        if (useSourceFileAsOutputFile)
        {
            sourceFileMap = SourceFileResolver.BuildTypeToSourceFileMap(assemblyFile);
            pendingContent = [];
        }

        await commands.Write(outputPath, typesInvolved, TemplateTypes.Command, directories, segmentsToSkip, "commands", message, errorMessage, generatedFiles, descriptorOrigins, sourceFileMap, pendingContent);

        var singleModelQueries = queries.Where(_ => !_.IsEnumerable && !_.IsObservable).ToList();
        await singleModelQueries.Write(outputPath, typesInvolved, TemplateTypes.Query, directories, segmentsToSkip, "single model queries", message, errorMessage, generatedFiles, descriptorOrigins, sourceFileMap, pendingContent);

        var enumerableQueries = queries.Where(_ => _.IsEnumerable && !_.IsObservable).ToList();
        await enumerableQueries.Write(outputPath, typesInvolved, TemplateTypes.Query, directories, segmentsToSkip, "queries", message, errorMessage, generatedFiles, descriptorOrigins, sourceFileMap, pendingContent);

        var observableQueries = queries.Where(_ => _.IsObservable).ToList();
        await observableQueries.Write(outputPath, typesInvolved, TemplateTypes.ObservableQuery, directories, segmentsToSkip, "observable queries", message, errorMessage, generatedFiles, descriptorOrigins, sourceFileMap, pendingContent);

        foreach (var identityDetailsType in identityDetailsTypesProvider.IdentityDetailsTypes)
        {
            identityDetailsType.CollectTypesInvolved(typesInvolved);
        }

        if (libraryMode)
        {
            var allPublicTypes = TypeExtensions.Assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.DefinedTypes.Where(t => (t.IsPublic && !t.IsAbstract) || t.IsEnum || t.IsInterface);
                    }
                    catch
                    {
                        return (IEnumerable<Type>)[];
                    }
                });

            foreach (var type in allPublicTypes)
            {
                type.CollectTypesInvolved(typesInvolved);
            }

            message($"  Library mode: {typesInvolved.Count} types collected");
        }

        typesInvolved = [.. typesInvolved.Distinct()];
        var enums = typesInvolved.Where(_ => _.IsEnum).ToList();

        var typeDescriptors = typesInvolved.Where(_ => !enums.Contains(_) && !_.IsFromMappedAssembly()).ToList().ConvertAll(_ => _.ToTypeDescriptor(outputPath, segmentsToSkip));
        await typeDescriptors.Write(outputPath, typesInvolved, TemplateTypes.Type, directories, segmentsToSkip, "types", message, errorMessage, generatedFiles, descriptorOrigins, sourceFileMap, pendingContent);

        var regularEnumDescriptors = enums
            .Where(_ => !_.IsFromMappedAssembly() && !_.IsFlagsEnum())
            .ToList()
            .ConvertAll(_ => _.ToEnumDescriptor());
        await regularEnumDescriptors.Write(outputPath, typesInvolved, TemplateTypes.Enum, directories, segmentsToSkip, "enums", message, errorMessage, generatedFiles, descriptorOrigins, sourceFileMap, pendingContent);

        var flagsEnumDescriptors = enums
            .Where(_ => !_.IsFromMappedAssembly() && _.IsFlagsEnum())
            .ToList()
            .ConvertAll(_ => _.ToEnumDescriptor());
        await flagsEnumDescriptors.Write(outputPath, typesInvolved, TemplateTypes.FlagsEnum, directories, segmentsToSkip, "flags enums", message, errorMessage, generatedFiles, descriptorOrigins, sourceFileMap, pendingContent);

        // Flush deferred content to disk, comparing final merged hashes against original files
        if (pendingContent is { Count: > 0 })
        {
            await DescriptorExtensions.FlushPendingContent(pendingContent, generatedFiles, message);
        }

        // Find and remove orphaned files
        var stopwatch = Stopwatch.StartNew();
        var orphanedFiles = FileMetadataScanner.FindOrphanedFiles(outputPath, generatedFiles);
        var removedCount = FileMetadataScanner.RemoveOrphanedFiles(outputPath, orphanedFiles, message);
        if (removedCount > 0)
        {
            message($"  Removed {removedCount} orphaned file(s) in {stopwatch.Elapsed}");
        }

        // Update index files intelligently
        if (!skipIndexGeneration)
        {
            stopwatch.Restart();
            var distinctDirectories = directories.Distinct().ToList();
            var (writtenCount, skippedCount) = IndexFileManager.UpdateAllIndexFiles(distinctDirectories, generatedFiles, orphanedFiles, message, outputPath);
            var skippedInfo = skippedCount > 0 ? $" ({skippedCount} unchanged)" : string.Empty;
            message($"  {writtenCount} index files written{skippedInfo} in {stopwatch.Elapsed}");
        }
        else
        {
            message("  Index file generation skipped");
        }

        message($"  Overall time: {overallStopwatch.Elapsed}");

        return true;
    }
}
