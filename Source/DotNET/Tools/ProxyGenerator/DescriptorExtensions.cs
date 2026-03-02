// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.ProxyGenerator.Templates;
using HandlebarsDotNet;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extension methods for <see cref="IDescriptor"/>.
/// </summary>
public static class DescriptorExtensions
{
    /// <summary>
    /// Write the descriptors to disk.
    /// </summary>
    /// <param name="descriptors">Descriptors to write.</param>
    /// <param name="targetPath">The target path to write relative to.</param>
    /// <param name="typesInvolved">Collection of types that are involved from any of the types written.</param>
    /// <param name="template">Template to use for writing.</param>
    /// <param name="directories">All directories that has been written to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="typeNameToEcho">The type name to echo for statistics.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="errorMessage">Logger to use for outputting error messages.</param>
    /// <param name="generatedFiles">Optional collection to track generated file paths and their metadata.</param>
    /// <param name="descriptorOrigins">Optional origins for descriptors to output contextual error information.</param>
    /// <param name="sourceFileMap">Optional map of type full name to source C# file name used to group descriptors into a single file per source file.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Write(
        this IEnumerable<IDescriptor> descriptors,
        string targetPath,
        IList<Type> typesInvolved,
        HandlebarsTemplate<object, object> template,
        IList<string> directories,
        int segmentsToSkip,
        string typeNameToEcho,
        Action<string> message,
        Action<string> errorMessage,
        IDictionary<string, GeneratedFileMetadata>? generatedFiles = null,
        IReadOnlyDictionary<Type, (IReadOnlyList<string> Commands, IReadOnlyList<string> Queries)>? descriptorOrigins = null,
        IReadOnlyDictionary<string, string>? sourceFileMap = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var generationTime = DateTime.UtcNow;
        var skippedCount = 0;

        // Build output path map: each path maps to a list of descriptors (one when no source file grouping, potentially many when grouping)
        var descriptorsByOutputPath = new Dictionary<string, List<IDescriptor>>();

        foreach (var descriptor in descriptors)
        {
            var path = descriptor.Type.ResolveTargetPath(segmentsToSkip);
            string outputFileName;

            if (sourceFileMap is not null && sourceFileMap.TryGetValue(descriptor.Type.FullName!, out var sourceFile))
            {
                outputFileName = sourceFile;
            }
            else
            {
                outputFileName = descriptor.Name;
            }

            var fullPath = Path.Join(targetPath, path, $"{outputFileName}.ts");
            var normalizedFullPath = Path.GetFullPath(fullPath);

            if (!descriptorsByOutputPath.TryGetValue(normalizedFullPath, out var group))
            {
                group = [];
                descriptorsByOutputPath[normalizedFullPath] = group;
            }

            group.Add(descriptor);
        }

        // When not using source file grouping, check for duplicate type names that map to the same path
        if (sourceFileMap is null)
        {
            var duplicates = new List<(string Path, IDescriptor First, IDescriptor Duplicate)>();

            foreach (var (path, group) in descriptorsByOutputPath)
            {
                if (group.Count > 1)
                {
                    duplicates.Add((path, group[0], group[1]));
                }
            }

            if (duplicates.Count != 0)
            {
                errorMessage(string.Empty);
                errorMessage($"ERROR: Duplicate type names detected in {typeNameToEcho} that would generate to the same file path:");
                errorMessage(string.Empty);

                foreach (var (path, first, duplicate) in duplicates)
                {
                    var firstOrigin = GetOriginDescription(first, descriptorOrigins);
                    var duplicateOrigin = GetOriginDescription(duplicate, descriptorOrigins);

                    errorMessage($"  Output path: {path}");
                    errorMessage($"    Type 1: {first.Type.FullName} (from {first.Type.Assembly.GetName().Name}){firstOrigin}");
                    errorMessage($"    Type 2: {duplicate.Type.FullName} (from {duplicate.Type.Assembly.GetName().Name}){duplicateOrigin}");
                    errorMessage(string.Empty);
                }
                Environment.Exit(1);
            }
        }

        foreach (var (normalizedFullPath, group) in descriptorsByOutputPath)
        {
            var directory = Path.GetDirectoryName(normalizedFullPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            if (!directories.Contains(directory))
            {
                directories.Add(directory);
            }

            string proxyContent;
            string sourceTypeName;

            if (group.Count == 1)
            {
                proxyContent = template(group[0]);
                sourceTypeName = group[0].Type.FullName!;
            }
            else
            {
                var contents = group.ConvertAll(d => template(d));
                proxyContent = TypeScriptContentCombiner.Combine(contents);
                sourceTypeName = string.Join(", ", group.ConvertAll(d => d.Type.FullName));
            }

            var contentHash = GeneratedFileMetadata.ComputeHash(proxyContent);
            var metadata = new GeneratedFileMetadata(sourceTypeName, generationTime, contentHash);

            // Check if file exists with the same hash
            if (File.Exists(normalizedFullPath) && GeneratedFileMetadata.IsGeneratedFile(normalizedFullPath, out var existingMetadata) &&
                    existingMetadata is not null &&
                    existingMetadata.ContentHash == contentHash)
            {
                // File hasn't changed, skip writing
                skippedCount++;
                if (generatedFiles is not null)
                {
                    // Mark as not written - preserve the existing timestamp
                    generatedFiles[normalizedFullPath] = new GeneratedFileMetadata(metadata.SourceTypeName, existingMetadata.GeneratedTime, metadata.ContentHash, false);
                }
                continue;
            }

            var contentWithMetadata = $"{metadata.ToCommentLine()}{Environment.NewLine}{proxyContent}";
            await File.WriteAllTextAsync(normalizedFullPath, contentWithMetadata);

            // Track generated file metadata and mark as written
            if (generatedFiles is not null)
            {
                generatedFiles[normalizedFullPath] = new GeneratedFileMetadata(metadata.SourceTypeName, metadata.GeneratedTime, metadata.ContentHash, true);
            }
        }

        foreach (var type in descriptors.SelectMany(_ => _.TypesInvolved))
        {
            if (!typesInvolved.Contains(type))
            {
                typesInvolved.Add(type);
            }
        }

        var count = descriptors.Count();
        if (count > 0)
        {
            var writtenCount = descriptorsByOutputPath.Count - skippedCount;
            var skippedInfo = skippedCount > 0 ? $" ({skippedCount} unchanged)" : string.Empty;
            message($"  {writtenCount} {typeNameToEcho} written{skippedInfo} in {stopwatch.Elapsed}");
        }
    }

    /// <summary>
    /// Build a descriptor origin map for types involved in commands and queries.
    /// </summary>
    /// <param name="commands">Commands to collect origins from.</param>
    /// <param name="queries">Queries to collect origins from.</param>
    /// <returns>Map of type to origin details.</returns>
    internal static IReadOnlyDictionary<Type, (IReadOnlyList<string> Commands, IReadOnlyList<string> Queries)> BuildDescriptorOrigins(
        IEnumerable<CommandDescriptor> commands,
        IEnumerable<QueryDescriptor> queries)
    {
        var origins = new Dictionary<Type, (HashSet<string> Commands, HashSet<string> Queries)>();

        void AddOrigin(Type type, string name, bool isCommand)
        {
            if (!origins.TryGetValue(type, out var existing))
            {
                existing = ([], []);
                origins[type] = existing;
            }

            if (isCommand)
            {
                existing.Commands.Add(name);
            }
            else
            {
                existing.Queries.Add(name);
            }
        }

        foreach (var command in commands)
        {
            foreach (var type in command.TypesInvolved)
            {
                AddOrigin(type, command.Name, true);
            }
        }

        foreach (var query in queries)
        {
            foreach (var type in query.TypesInvolved)
            {
                AddOrigin(type, query.Name, false);
            }
        }

        return origins.ToDictionary(
            _ => _.Key,
            _ => (
                Commands: (IReadOnlyList<string>)_.Value.Commands.Order().ToList(),
                Queries: (IReadOnlyList<string>)_.Value.Queries.Order().ToList()));
    }

    /// <summary>
    /// Get an origin description for a descriptor, if available.
    /// </summary>
    /// <param name="descriptor">Descriptor to get origin for.</param>
    /// <param name="descriptorOrigins">Optional map of type origins.</param>
    /// <returns>Formatted origin description, prefixed with a space if present.</returns>
    static string GetOriginDescription(IDescriptor descriptor, IReadOnlyDictionary<Type, (IReadOnlyList<string> Commands, IReadOnlyList<string> Queries)>? descriptorOrigins)
    {
        var description = descriptor switch
        {
            CommandDescriptor command => $"from command {command.Name}",
            QueryDescriptor query => $"from query {query.Name}",
            _ => descriptorOrigins is not null && descriptorOrigins.TryGetValue(descriptor.Type, out var origins)
                ? GetOriginsDescription(origins.Commands, origins.Queries)
                : string.Empty
        };

        return string.IsNullOrWhiteSpace(description) ? string.Empty : $" ({description})";
    }

    /// <summary>
    /// Convert origins to a readable description.
    /// </summary>
    /// <param name="commands">Commands that reference the type.</param>
    /// <param name="queries">Queries that reference the type.</param>
    /// <returns>Readable description.</returns>
    static string GetOriginsDescription(IReadOnlyList<string> commands, IReadOnlyList<string> queries)
    {
        var parts = new List<string>();

        if (commands.Count == 1)
        {
            parts.Add($"from command {commands[0]}");
        }
        else if (commands.Count > 1)
        {
            parts.Add($"from commands {string.Join(", ", commands)}");
        }

        if (queries.Count == 1)
        {
            parts.Add($"from query {queries[0]}");
        }
        else if (queries.Count > 1)
        {
            parts.Add($"from queries {string.Join(", ", queries)}");
        }

        return string.Join("; ", parts);
    }
}
