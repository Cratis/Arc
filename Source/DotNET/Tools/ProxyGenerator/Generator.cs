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
    /// <param name="skipOutputDeletion">True if the output path should be deleted before generating, false if not.</param>
    /// <param name="skipCommandNameInRoute">True if the command name should be skipped in the route, false if not.</param>
    /// <param name="skipQueryNameInRoute">True if the query name should be skipped in the route, false if not.</param>
    /// <param name="apiPrefix">The API prefix to use in the route.</param>
    /// <param name="skipIndexGeneration">True to skip index.ts file generation, false to enable it.</param>
    /// <returns>True if successful, false if not.</returns>
    public static async Task<bool> Generate(
        string assemblyFile,
        string outputPath,
        int segmentsToSkip,
        Action<string> message,
        Action<string> errorMessage,
        bool skipOutputDeletion = false,
        bool skipCommandNameInRoute = false,
        bool skipQueryNameInRoute = false,
        string apiPrefix = "api",
        bool skipIndexGeneration = false)
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

        message($"  Found {commands.Count} commands and {queries.Count} queries");

        if (Directory.Exists(outputPath) && !skipOutputDeletion) Directory.Delete(outputPath, true);
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        var typesInvolved = new List<Type>();
        var directories = new List<string>();
        var generatedFiles = new Dictionary<string, GeneratedFileMetadata>();

        await commands.Write(outputPath, typesInvolved, TemplateTypes.Command, directories, segmentsToSkip, "commands", message, generatedFiles);

        var singleModelQueries = queries.Where(_ => !_.IsEnumerable && !_.IsObservable).ToList();
        await singleModelQueries.Write(outputPath, typesInvolved, TemplateTypes.Query, directories, segmentsToSkip, "single model queries", message, generatedFiles);

        var enumerableQueries = queries.Where(_ => _.IsEnumerable && !_.IsObservable).ToList();
        await enumerableQueries.Write(outputPath, typesInvolved, TemplateTypes.Query, directories, segmentsToSkip, "queries", message, generatedFiles);

        var observableQueries = queries.Where(_ => _.IsObservable).ToList();
        await observableQueries.Write(outputPath, typesInvolved, TemplateTypes.ObservableQuery, directories, segmentsToSkip, "observable queries", message, generatedFiles);

        foreach (var identityDetailsType in identityDetailsTypesProvider.IdentityDetailsTypes)
        {
            identityDetailsType.CollectTypesInvolved(typesInvolved);
        }

        typesInvolved = [.. typesInvolved.Distinct()];
        var enums = typesInvolved.Where(_ => _.IsEnum).ToList();

        var typeDescriptors = typesInvolved.Where(_ => !enums.Contains(_)).ToList().ConvertAll(_ => _.ToTypeDescriptor(outputPath, segmentsToSkip));
        await typeDescriptors.Write(outputPath, typesInvolved, TemplateTypes.Type, directories, segmentsToSkip, "types", message, generatedFiles);

        var enumDescriptors = enums.ConvertAll(_ => _.ToEnumDescriptor());
        await enumDescriptors.Write(outputPath, typesInvolved, TemplateTypes.Enum, directories, segmentsToSkip, "enums", message, generatedFiles);

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
