// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Conservatively follows relative module dependencies to prevent indirectly mounted tables becoming absolute roots.
/// </summary>
/// <param name="files">The module content source.</param>
/// <param name="contents">The already read TSX files.</param>
internal sealed partial class RouteModuleReferences(IUserInterfaceFiles files, IReadOnlyDictionary<string, string?> contents)
{
    readonly Dictionary<string, string?> _modules = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets every module reachable through imports, re-exports or literal dynamic imports of table modules.
    /// </summary>
    /// <param name="tables">The table module paths.</param>
    /// <returns>Potential nested modules, with cycles visited only once.</returns>
    internal HashSet<string> NestedIn(IEnumerable<string> tables)
    {
        var nested = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>(tables.SelectMany(Dependencies));
        while (pending.TryDequeue(out var module))
        {
            var key = Module(module) is { } found ? RouteElements.WithoutExtension(found.Path) : module;
            if (nested.Add(key))
            {
                foreach (var dependency in Dependencies(module))
                {
                    pending.Enqueue(dependency);
                }
            }
        }

        return nested;
    }

    // False-positive dependencies only remove mappings; they can never create a navigation edge.
    [GeneratedRegex("""\b(?:from\s*|import\s*\(\s*|require\s*\(\s*)(?<quote>['"])(?<module>\.{1,2}/[^'"\\]*)\k<quote>""", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ModuleRegex();

    IEnumerable<string> Dependencies(string path)
    {
        var module = Module(path);
        return module is { } found ? ModuleRegex().Matches(found.Text).Cast<Match>()
            .Select(_ => ModulePaths.Resolve(ScreenFiles.DirectoryOf(found.Path), _.Groups["module"].Value))
            .OfType<string>().Select(RouteElements.WithoutExtension) : [];
    }

    (string Path, string Text)? Module(string path)
    {
        var bare = RouteElements.WithoutExtension(path);
        foreach (var candidate in new[] { path, bare + ".tsx", bare + ".ts", bare + "/index.tsx", bare + "/index.ts" }.Distinct(StringComparer.Ordinal))
        {
            if (!_modules.TryGetValue(candidate, out var text))
            {
                text = contents.TryGetValue(candidate, out var read) ? read : files.Contents(candidate);
                _modules.Add(candidate, text);
            }

            if (text is not null)
            {
                return (candidate, text);
            }
        }

        return null;
    }
}
