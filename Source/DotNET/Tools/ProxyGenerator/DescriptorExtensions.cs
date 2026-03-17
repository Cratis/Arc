// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Cratis.Arc.ProxyGenerator.Templates;
using HandlebarsDotNet;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extension methods for <see cref="IDescriptor"/>.
/// </summary>
public static partial class DescriptorExtensions
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
    /// <param name="pendingContent">Optional dictionary to accumulate content for deferred writing when using source file grouping. When provided, content is stored here instead of being written to disk immediately. Use <see cref="FlushPendingContent"/> to write the final merged content.</param>
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
        IReadOnlyDictionary<string, string>? sourceFileMap = null,
        IDictionary<string, (string Content, string SourceTypeName)>? pendingContent = null)
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

        // Build import path fixup map when using source file grouping.
        // This maps type short names to their source file names so import module paths
        // reference the correct output file instead of the type name.
        Dictionary<string, string>? importPathFixups = null;
        if (sourceFileMap is not null)
        {
            importPathFixups = BuildImportPathFixups(sourceFileMap);
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

            // Fix import module paths when using source file grouping so that imports
            // reference the source-file-based output name instead of the type name.
            if (importPathFixups is { Count: > 0 })
            {
                proxyContent = FixImportModulePaths(proxyContent, importPathFixups);
            }

            // When using deferred writing (source file grouping), accumulate content in memory
            // so that cross-category merging happens without touching disk. This prevents the
            // first Write call from overwriting the merged file with partial content, which would
            // force every subsequent call to rewrite the file even when the final content is unchanged.
            if (pendingContent is not null)
            {
                if (pendingContent.TryGetValue(normalizedFullPath, out var pending))
                {
                    proxyContent = TypeScriptContentCombiner.Combine([pending.Content, proxyContent]);
                    sourceTypeName = $"{pending.SourceTypeName}, {sourceTypeName}";
                }

                pendingContent[normalizedFullPath] = (proxyContent, sourceTypeName);

                // Track in generatedFiles so orphan detection knows about this file
                if (generatedFiles is not null)
                {
                    var contentHash = GeneratedFileMetadata.ComputeHash(proxyContent);
                    generatedFiles[normalizedFullPath] = new GeneratedFileMetadata(sourceTypeName, generationTime, contentHash, false);
                }
            }
            else
            {
                // When using source file grouping and a previous Write call already produced content
                // for this same output file (cross-category collision), merge the contents.
                if (sourceFileMap is not null &&
                    generatedFiles is not null &&
                    generatedFiles.TryGetValue(normalizedFullPath, out var previousMetadata) &&
                    File.Exists(normalizedFullPath))
                {
                    var existingFileContent = await File.ReadAllTextAsync(normalizedFullPath);
                    var existingProxyContent = StripMetadataLine(existingFileContent);
                    proxyContent = TypeScriptContentCombiner.Combine([existingProxyContent, proxyContent]);
                    sourceTypeName = $"{previousMetadata.SourceTypeName}, {sourceTypeName}";
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
    /// Writes all accumulated pending content to disk, comparing each file's final merged hash
    /// against the original file on disk to avoid unnecessary rewrites and timestamp changes.
    /// </summary>
    /// <param name="pendingContent">The accumulated pending content from all Write calls.</param>
    /// <param name="generatedFiles">The generated files metadata dictionary to update.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task FlushPendingContent(
        IDictionary<string, (string Content, string SourceTypeName)> pendingContent,
        IDictionary<string, GeneratedFileMetadata> generatedFiles,
        Action<string> message)
    {
        var generationTime = DateTime.UtcNow;
        var writtenCount = 0;
        var skippedCount = 0;

        foreach (var (normalizedFullPath, (content, sourceTypeName)) in pendingContent)
        {
            var contentHash = GeneratedFileMetadata.ComputeHash(content);

            // Compare against the original file on disk (untouched since last build)
            if (File.Exists(normalizedFullPath) &&
                GeneratedFileMetadata.IsGeneratedFile(normalizedFullPath, out var existingMetadata) &&
                existingMetadata is not null &&
                existingMetadata.ContentHash == contentHash)
            {
                // File hasn't changed, skip writing and preserve the existing timestamp
                skippedCount++;
                generatedFiles[normalizedFullPath] = new GeneratedFileMetadata(sourceTypeName, existingMetadata.GeneratedTime, contentHash, false);
                continue;
            }

            var metadata = new GeneratedFileMetadata(sourceTypeName, generationTime, contentHash);
            var contentWithMetadata = $"{metadata.ToCommentLine()}{Environment.NewLine}{content}";
            await File.WriteAllTextAsync(normalizedFullPath, contentWithMetadata);

            writtenCount++;
            generatedFiles[normalizedFullPath] = new GeneratedFileMetadata(sourceTypeName, generationTime, contentHash, true);
        }

        var skippedInfo = skippedCount > 0 ? $" ({skippedCount} unchanged)" : string.Empty;
        message($"  {writtenCount} deferred file(s) written{skippedInfo}");
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

    /// <summary>
    /// Strips the metadata comment line from previously generated file content.
    /// </summary>
    /// <param name="fileContent">The full file content including the metadata line.</param>
    /// <returns>The file content without the leading metadata line.</returns>
    static string StripMetadataLine(string fileContent)
    {
        var newlineIndex = fileContent.IndexOf('\n');
        if (newlineIndex < 0)
        {
            return fileContent;
        }

        var firstLine = fileContent[..newlineIndex].TrimEnd('\r');
        if (!firstLine.StartsWith("// @generated by Cratis", StringComparison.Ordinal))
        {
            return fileContent;
        }

        return fileContent[(newlineIndex + 1)..];
    }

    /// <summary>
    /// Builds a lookup of type short names to source file names for types where the names differ.
    /// </summary>
    /// <param name="sourceFileMap">Map of full type name to source file name.</param>
    /// <returns>Dictionary mapping type short name to source file name.</returns>
    static Dictionary<string, string> BuildImportPathFixups(IReadOnlyDictionary<string, string> sourceFileMap)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var conflicts = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (fullName, sourceFile) in sourceFileMap)
        {
            var shortName = fullName.Split('.')[^1];
            if (shortName == sourceFile || conflicts.Contains(shortName))
            {
                continue;
            }

            if (result.TryGetValue(shortName, out var existing))
            {
                if (existing != sourceFile)
                {
                    result.Remove(shortName);
                    conflicts.Add(shortName);
                }
            }
            else
            {
                result[shortName] = sourceFile;
            }
        }

        return result;
    }

    /// <summary>
    /// Fixes import module paths in rendered TypeScript content to use source file names instead of type names.
    /// </summary>
    /// <param name="content">The rendered TypeScript content.</param>
    /// <param name="fixups">Map of type short name to source file name.</param>
    /// <returns>Content with corrected import module paths.</returns>
    static string FixImportModulePaths(string content, Dictionary<string, string> fixups) =>
        ImportModulePathRegex().Replace(content, match =>
        {
            var prefix = match.Groups["prefix"].Value;
            var fileName = match.Groups["fileName"].Value;

            if (!prefix.StartsWith('.'))
            {
                return match.Value;
            }

            return fixups.TryGetValue(fileName, out var sourceFile)
                ? $"from '{prefix}{sourceFile}'"
                : match.Value;
        });

    [GeneratedRegex(@"from\s+'(?<prefix>[^']*/)(?<fileName>[^'/]+)'", RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex ImportModulePathRegex();
}
