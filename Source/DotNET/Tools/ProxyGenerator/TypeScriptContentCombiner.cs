// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Combines multiple generated TypeScript file contents into a single file.
/// </summary>
public static partial class TypeScriptContentCombiner
{
    /// <summary>
    /// Combines multiple TypeScript file contents into a single file content.
    /// The DO NOT EDIT header is taken from the first content, preamble lines (ESLint directives
    /// and import statements) are deduplicated, and all export declarations are concatenated.
    /// </summary>
    /// <param name="contents">The TypeScript file contents to combine.</param>
    /// <returns>The combined TypeScript file content.</returns>
    public static string Combine(IEnumerable<string> contents)
    {
        var contentList = contents.ToList();

        if (contentList.Count == 0)
        {
            return string.Empty;
        }

        if (contentList.Count == 1)
        {
            return contentList[0];
        }

        var parsed = contentList.ConvertAll(ParseContent);

        var header = parsed[0].Header;

        var seenPreambleLines = new HashSet<string>(StringComparer.Ordinal);
        var eslintLines = new List<string>();
        var importLines = new List<string>();

        foreach (var section in parsed)
        {
            foreach (var line in section.PreambleLines)
            {
                if (!seenPreambleLines.Add(line))
                {
                    continue;
                }

                if (line.TrimStart().StartsWith("import ", StringComparison.Ordinal))
                {
                    importLines.Add(line);
                }
                else
                {
                    eslintLines.Add(line);
                }
            }
        }

        var bodies = parsed
            .Select(s => s.Body.Trim())
            .Where(b => !string.IsNullOrEmpty(b))
            .ToList();

        bodies = TopologicallySortBodies(bodies);

        var exportedTypes = CollectExportedTypeNames(bodies);
        var filteredImportLines = importLines
            .Where(line => !IsImportForExportedType(line, exportedTypes))
            .ToList();

        var sb = new StringBuilder()
            .AppendLine(header);

        foreach (var line in eslintLines)
        {
            sb.AppendLine(line);
        }

        foreach (var line in filteredImportLines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine()
          .AppendJoin($"{Environment.NewLine}{Environment.NewLine}", bodies)
          .AppendLine();

        return sb.ToString();
    }

    static ParsedContent ParseContent(string content)
    {
        var lines = content.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        // Find the first line that is a code declaration (exported or non-exported class, interface, etc.)
        var codeStartIndex = lines.FindIndex(l =>
            l.StartsWith("export ", StringComparison.Ordinal) ||
            l.StartsWith("class ", StringComparison.Ordinal));

        if (codeStartIndex < 0)
        {
            return new ParsedContent(string.Empty, [], content);
        }

        var header = string.Join('\n', lines.Take(3));

        // Walk backwards from the code start to include any preceding JSDoc block in the body,
        // so that per-type documentation is not mixed into the shared preamble section.
        var bodyStartIndex = codeStartIndex;
        var checkIndex = codeStartIndex - 1;

        while (checkIndex >= 0 && string.IsNullOrWhiteSpace(lines[checkIndex]))
        {
            checkIndex--;
        }

        if (checkIndex >= 0 && lines[checkIndex].TrimStart().StartsWith("*/", StringComparison.Ordinal))
        {
            var scanIndex = checkIndex;
            while (scanIndex >= 0 && !lines[scanIndex].TrimStart().StartsWith("/**", StringComparison.Ordinal))
            {
                scanIndex--;
            }

            if (scanIndex >= 0 && lines[scanIndex].TrimStart().StartsWith("/**", StringComparison.Ordinal))
            {
                bodyStartIndex = scanIndex;
            }
        }

        var preambleLines = lines
            .Skip(3)
            .Take(bodyStartIndex - 3)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var body = string.Join('\n', lines.Skip(bodyStartIndex));

        return new ParsedContent(header, preambleLines, body);
    }

    /// <summary>
    /// Collects the names of all exported types (class, interface, enum, type, const enum) from the body sections.
    /// </summary>
    /// <param name="bodies">The body sections of the combined TypeScript content.</param>
    /// <returns>A set of exported type names.</returns>
    static HashSet<string> CollectExportedTypeNames(IReadOnlyList<string> bodies)
    {
        var exportPattern = ExportDeclarationRegex();
        var typeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var body in bodies)
        {
            foreach (Match match in exportPattern.Matches(body))
            {
                typeNames.Add(match.Groups["name"].Value);
            }
        }

        return typeNames;
    }

    /// <summary>
    /// Determines whether an import line imports a type that is defined in the same combined file.
    /// </summary>
    /// <param name="importLine">The import line to check.</param>
    /// <param name="exportedTypes">The set of type names exported within the combined file.</param>
    /// <returns>True if the import is for a type defined in the same file, false otherwise.</returns>
    static bool IsImportForExportedType(string importLine, HashSet<string> exportedTypes)
    {
        var match = ImportedTypeRegex().Match(importLine);
        if (!match.Success)
        {
            return false;
        }

        var importedNames = match.Groups["names"].Value;

        return importedNames
            .Split(',')
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .All(exportedTypes.Contains);
    }

    [GeneratedRegex(@"export\s+(?:class|interface|enum|type|const\s+enum)\s+(?<name>\w+)", RegexOptions.NonBacktracking)]
    private static partial Regex ExportDeclarationRegex();

    [GeneratedRegex(@"import\s+\{\s*(?<names>[^}]+)\s*\}\s+from\s+", RegexOptions.NonBacktracking)]
    private static partial Regex ImportedTypeRegex();

    [GeneratedRegex(@"@field\(\s*(?<type>[A-Z]\w*)", RegexOptions.NonBacktracking)]
    private static partial Regex FieldDecoratorReferenceRegex();

    /// <summary>
    /// Topologically sorts body sections so that types referenced by <c>@field</c> decorators
    /// in other bodies appear before the bodies that reference them. This prevents forward
    /// reference errors at runtime since TypeScript classes are not hoisted.
    /// </summary>
    /// <param name="bodies">The body sections to sort.</param>
    /// <returns>A topologically sorted list of body sections.</returns>
    static List<string> TopologicallySortBodies(List<string> bodies)
    {
        if (bodies.Count <= 1)
        {
            return bodies;
        }

        var exportPattern = ExportDeclarationRegex();
        var fieldPattern = FieldDecoratorReferenceRegex();
        var builtInTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "String", "Number", "Boolean", "Date", "Object"
        };

        // Map each body to its exported type names and the custom types it references via @field.
        var bodyExports = new List<HashSet<string>>();
        var bodyDependencies = new List<HashSet<string>>();

        foreach (var body in bodies)
        {
            var exports = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in exportPattern.Matches(body))
            {
                exports.Add(match.Groups["name"].Value);
            }

            var dependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in fieldPattern.Matches(body))
            {
                var typeName = match.Groups["type"].Value;
                if (!builtInTypes.Contains(typeName) && !exports.Contains(typeName))
                {
                    dependencies.Add(typeName);
                }
            }

            bodyExports.Add(exports);
            bodyDependencies.Add(dependencies);
        }

        // Build adjacency: body i depends on body j if j exports a type that i references.
        var inDegree = new int[bodies.Count];
        var dependents = new List<List<int>>();
        for (var i = 0; i < bodies.Count; i++)
        {
            dependents.Add([]);
        }

        for (var i = 0; i < bodies.Count; i++)
        {
            foreach (var dep in bodyDependencies[i])
            {
                for (var j = 0; j < bodies.Count; j++)
                {
                    if (i != j && bodyExports[j].Contains(dep))
                    {
                        dependents[j].Add(i);
                        inDegree[i]++;
                    }
                }
            }
        }

        // Kahn's algorithm for topological sort.
        var queue = new Queue<int>();
        for (var i = 0; i < bodies.Count; i++)
        {
            if (inDegree[i] == 0)
            {
                queue.Enqueue(i);
            }
        }

        var sorted = new List<string>(bodies.Count);
        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            sorted.Add(bodies[index]);
            foreach (var dependent in dependents[index])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        // If there is a cycle, fall back to original order for any remaining bodies.
        if (sorted.Count < bodies.Count)
        {
            for (var i = 0; i < bodies.Count; i++)
            {
                if (inDegree[i] > 0)
                {
                    sorted.Add(bodies[i]);
                }
            }
        }

        return sorted;
    }

    sealed record ParsedContent(string Header, IReadOnlyList<string> PreambleLines, string Body);
}
