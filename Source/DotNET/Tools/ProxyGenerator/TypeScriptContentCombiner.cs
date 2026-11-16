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

        var (eslintLines, importLines) = MergePreamble(parsed);
        var bodies = parsed
            .Select(s => s.Body.Trim())
            .Where(b => !string.IsNullOrEmpty(b))
            .ToList();

        bodies = TopologicallySortBodies(bodies);

        var exportedTypes = CollectExportedTypeNames(bodies);
        var filteredImportLines = importLines
            .Where(line => !IsImportForExportedType(line, exportedTypes))
            .ToList();

        return RenderContent(parsed[0].Header, eslintLines, filteredImportLines, bodies);
    }

    static (List<string> EslintLines, List<string> ImportLines) MergePreamble(IEnumerable<ParsedContent> parsed)
    {
        var seenPreambleLines = new HashSet<string>(StringComparer.Ordinal);
        var eslintLines = new List<string>();

        // Map module path → ordered set of named imports, preserving insertion order per module.
        var namedImportsByModule = new Dictionary<string, LinkedList<string>>(StringComparer.Ordinal);

        // Preserve the order in which modules are first encountered.
        var moduleOrder = new List<string>();

        // Non-named imports (side-effect, default, namespace) deduplicated by exact string.
        var otherImportLines = new List<string>();

        foreach (var line in parsed.SelectMany(section => section.PreambleLines).Where(seenPreambleLines.Add))
        {
            if (!line.TrimStart().StartsWith("import ", StringComparison.Ordinal))
            {
                eslintLines.Add(line);
                continue;
            }

            var namedMatch = NamedImportRegex().Match(line);
            if (!namedMatch.Success)
            {
                otherImportLines.Add(line);
                continue;
            }

            AddNamedImport(namedMatch, namedImportsByModule, moduleOrder);
        }

        // Reconstruct merged import lines in module encounter order.
        var importLines = new List<string>();
        foreach (var modulePath in moduleOrder)
        {
            // Sort bindings, not modules: changing module order can change evaluation in an import cycle.
            // Bindings sort by their local name, so an aliased binding ('X as Y') sorts as 'Y'.
            var names = namedImportsByModule[modulePath].OrderBy(LocalNameOf, StringComparer.Ordinal);
            importLines.Add($"import {{ {string.Join(", ", names)} }} from '{modulePath}';");
        }
        importLines.AddRange(otherImportLines);

        return (eslintLines, importLines);
    }

    static void AddNamedImport(Match namedMatch, Dictionary<string, LinkedList<string>> namedImportsByModule, List<string> moduleOrder)
    {
        var modulePath = namedMatch.Groups["module"].Value;
        var namesRaw = namedMatch.Groups["names"].Value;
        var names = namesRaw
            .Split(',')
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrEmpty(n));

        if (!namedImportsByModule.TryGetValue(modulePath, out var existing))
        {
            existing = new LinkedList<string>();
            namedImportsByModule[modulePath] = existing;
            moduleOrder.Add(modulePath);
        }

        foreach (var name in names.Where(name => !existing.Contains(name)))
        {
            existing.AddLast(name);
        }
    }

    static string RenderContent(string header, IEnumerable<string> eslintLines, IEnumerable<string> importLines, List<string> bodies)
    {
        var sb = new StringBuilder()
            .AppendLine(header);

        foreach (var line in eslintLines)
        {
            sb.AppendLine(line);
        }

        foreach (var line in importLines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine()
          .AppendJoin($"{Environment.NewLine}{Environment.NewLine}", bodies)
          .AppendLine();

        return sb.ToString();
    }

    static string LocalNameOf(string binding)
    {
        var aliasIndex = binding.LastIndexOf(" as ", StringComparison.Ordinal);
        return aliasIndex < 0 ? binding : binding[(aliasIndex + 4)..].Trim();
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

        var headerLength = lines.TakeWhile(line => string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal)).Count();
        var header = string.Join('\n', lines.Take(headerLength)).TrimEnd();

        var bodyStartIndex = FindBodyStartIndex(lines, codeStartIndex);

        var preambleLines = lines
            .Skip(headerLength)
            .Take(bodyStartIndex - headerLength)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var body = string.Join('\n', lines.Skip(bodyStartIndex));

        return new ParsedContent(header, preambleLines, body);
    }

    static int FindBodyStartIndex(List<string> lines, int codeStartIndex)
    {
        // Walk backwards to include decorators and preceding JSDoc in the body rather than the shared preamble.
        var bodyStartIndex = codeStartIndex;
        var checkIndex = SkipBlankLines(lines, codeStartIndex - 1);

        while (checkIndex >= 0 && lines[checkIndex].TrimStart().StartsWith('@'))
        {
            bodyStartIndex = checkIndex;
            checkIndex = SkipBlankLines(lines, checkIndex - 1);
        }

        return FindPrecedingDocumentation(lines, checkIndex, bodyStartIndex);
    }

    static int SkipBlankLines(List<string> lines, int index)
    {
        while (index >= 0 && string.IsNullOrWhiteSpace(lines[index]))
        {
            index--;
        }

        return index;
    }

    static int FindPrecedingDocumentation(List<string> lines, int checkIndex, int bodyStartIndex)
    {
        if (checkIndex < 0 || !lines[checkIndex].TrimStart().StartsWith("*/", StringComparison.Ordinal))
        {
            return bodyStartIndex;
        }

        var scanIndex = checkIndex;
        while (scanIndex >= 0 && !lines[scanIndex].TrimStart().StartsWith("/**", StringComparison.Ordinal))
        {
            scanIndex--;
        }

        return scanIndex >= 0 ? scanIndex : bodyStartIndex;
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

    [GeneratedRegex(@"import\s+\{\s*(?<names>[^}]+)\s*\}\s+from\s+'(?<module>[^']+)'", RegexOptions.NonBacktracking)]
    private static partial Regex NamedImportRegex();

    [GeneratedRegex(@"import\s+\{\s*(?<names>[^}]+)\s*\}\s+from\s+", RegexOptions.NonBacktracking)]
    private static partial Regex ImportedTypeRegex();

    [GeneratedRegex(@"^[ \t]*@field\((?<arguments>[^)\r\n]*)\)[ \t]*$", RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex FieldDecoratorRegex();

    [GeneratedRegex(@"\b(?<type>[A-Z]\w*)\b", RegexOptions.NonBacktracking)]
    private static partial Regex DecoratorTypeReferenceRegex();

    [GeneratedRegex(@"[!?]\s*:\s*(?<type>[A-Z]\w*)", RegexOptions.NonBacktracking)]
    private static partial Regex PropertyTypeAnnotationRegex();

    [GeneratedRegex(@"\bextends\s+(?<type>[A-Z]\w*)", RegexOptions.NonBacktracking)]
    private static partial Regex ExtendsClauseRegex();

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

        var (bodyExports, bodyDependencies) = CollectBodyReferences(bodies);
        var graph = BuildDependencyGraph(bodyExports, bodyDependencies);

        // Kahn's algorithm uses a FIFO queue and the original adjacency order.
        var queue = FindIndependentBodies(graph.InDegree);
        var sorted = new List<string>(bodies.Count);
        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            sorted.Add(bodies[index]);
            EnqueueReadyDependents(graph.Dependents[index], graph.InDegree, queue);
        }

        AppendUnresolvedBodies(bodies, sorted, graph.InDegree);

        return sorted;
    }

    static (List<HashSet<string>> Exports, List<HashSet<string>> Dependencies) CollectBodyReferences(IEnumerable<string> bodies)
    {
        var exportPattern = ExportDeclarationRegex();
        var builtInTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "String", "Number", "Boolean", "Date", "Object"
        };
        var bodyExports = new List<HashSet<string>>();
        var bodyDependencies = new List<HashSet<string>>();

        foreach (var body in bodies)
        {
            var exports = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in exportPattern.Matches(body))
            {
                exports.Add(match.Groups["name"].Value);
            }

            bodyExports.Add(exports);
            bodyDependencies.Add(CollectBodyDependencies(body, exports, builtInTypes));
        }

        return (bodyExports, bodyDependencies);
    }

    static HashSet<string> CollectBodyDependencies(string body, HashSet<string> exports, HashSet<string> builtInTypes)
    {
        var dependencies = new HashSet<string>(StringComparer.Ordinal);
        var decoratedTypeNames = FieldDecoratorRegex()
            .Matches(body)
            .SelectMany(match => DecoratorTypeReferenceRegex().Matches(match.Groups["arguments"].Value))
            .Select(match => match.Groups["type"].Value);
        var annotatedTypeNames = PropertyTypeAnnotationRegex().Matches(body).Select(match => match.Groups["type"].Value);

        // Treat `extends BaseType` as a dependency so that the base class body is ordered
        // before the derived class body when both are combined into the same file.
        var baseTypeNames = ExtendsClauseRegex().Matches(body).Select(match => match.Groups["type"].Value);
        var referencedTypeNames = decoratedTypeNames
            .Concat(annotatedTypeNames)
            .Concat(baseTypeNames)
            .Where(typeName => !builtInTypes.Contains(typeName) && !exports.Contains(typeName));

        foreach (var typeName in referencedTypeNames)
        {
            dependencies.Add(typeName);
        }

        return dependencies;
    }

    static (int[] InDegree, List<List<int>> Dependents) BuildDependencyGraph(List<HashSet<string>> bodyExports, List<HashSet<string>> bodyDependencies)
    {
        var graph = (InDegree: new int[bodyExports.Count], Dependents: new List<List<int>>());
        for (var i = 0; i < bodyExports.Count; i++)
        {
            graph.Dependents.Add([]);
        }

        for (var i = 0; i < bodyExports.Count; i++)
        {
            AddDependencyEdges(i, bodyExports, bodyDependencies[i], graph);
        }

        return graph;
    }

    static void AddDependencyEdges(int bodyIndex, List<HashSet<string>> bodyExports, HashSet<string> dependencies, (int[] InDegree, List<List<int>> Dependents) graph)
    {
        // Preserve dependency and exporter iteration order, including multiple edges between bodies.
        foreach (var dependency in dependencies)
        {
            for (var j = 0; j < bodyExports.Count; j++)
            {
                if (bodyIndex != j && bodyExports[j].Contains(dependency))
                {
                    graph.Dependents[j].Add(bodyIndex);
                    graph.InDegree[bodyIndex]++;
                }
            }
        }
    }

    static Queue<int> FindIndependentBodies(int[] inDegree)
    {
        var queue = new Queue<int>();
        for (var i = 0; i < inDegree.Length; i++)
        {
            if (inDegree[i] == 0)
            {
                queue.Enqueue(i);
            }
        }

        return queue;
    }

    static void EnqueueReadyDependents(List<int> dependents, int[] inDegree, Queue<int> queue)
    {
        foreach (var dependent in dependents)
        {
            inDegree[dependent]--;
            if (inDegree[dependent] == 0)
            {
                queue.Enqueue(dependent);
            }
        }
    }

    static void AppendUnresolvedBodies(List<string> bodies, List<string> sorted, int[] inDegree)
    {
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
    }

    sealed record ParsedContent(string Header, IReadOnlyList<string> PreambleLines, string Body);
}
