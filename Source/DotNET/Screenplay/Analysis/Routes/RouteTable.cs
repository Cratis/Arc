// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Collects readable route claims and a single, explicit omission report for one imported router table.
/// </summary>
/// <param name="path">The original module path.</param>
/// <param name="source">The scanned module.</param>
/// <param name="imports">Its import bindings.</param>
/// <param name="screens">The retained screen files.</param>
internal sealed class RouteTable(string path, TsxSource source, IReadOnlyDictionary<string, ScreenImportBinding> imports, IReadOnlyList<RouteScreen> screens)
{
    readonly RouteElements _elements = new(path, imports, screens);
    readonly List<RouteEntry> _entries = [];
    readonly HashSet<string> _issues = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the module path.
    /// </summary>
    internal string Path => path;

    /// <summary>
    /// Gets whether the table's lexical containers prove it creates or renders its own router.
    /// </summary>
    internal bool IsExplicitRoot => RouteRootProof.IsExplicitRoot(source, imports);

    /// <summary>
    /// Gets the literal route claims, including unread destinations.
    /// </summary>
    internal IReadOnlyList<RouteEntry> Entries => _entries;

    /// <summary>
    /// Gets the omission reasons, reported together once per module.
    /// </summary>
    internal IEnumerable<string> Issues => _issues.Order(StringComparer.Ordinal);

    /// <summary>
    /// Identifies router imports by their owning package and exported name.
    /// </summary>
    /// <param name="binding">The import binding.</param>
    /// <returns>Whether it is a supported route-table import.</returns>
    internal static bool IsRouter(ScreenImportBinding binding) =>
        RouteRootProof.IsRouterPackage(binding) && (string.Equals(binding.Name, "Route", StringComparison.Ordinal) ||
            string.Equals(binding.Name, "Routes", StringComparison.Ordinal) || RouteRootProof.IsFactory(binding));

    /// <summary>
    /// Reads all literal table roots in this module.
    /// </summary>
    internal void Read()
    {
        if (source.Problem is { } problem)
        {
            Omit($"unscannable {problem.Construct} at line {problem.Line}");
            _entries.Add(new(null, null));
            return;
        }

        foreach (var node in RouteTableRoots.Read(source, imports, Omit))
        {
            ReadNode(node, "/");
        }
    }

    /// <summary>
    /// Adds an omission to the per-table report.
    /// </summary>
    /// <param name="reason">What could not be read.</param>
    internal void Omit(string reason) => _issues.Add(reason);

    /// <summary>
    /// Retains an unknown mount as a competing claim rather than dropping its routes.
    /// </summary>
    /// <param name="reason">The unread mount reason.</param>
    internal void UnknownMount(string reason)
    {
        Omit(reason);
        for (var index = 0; index < _entries.Count; index++)
        {
            _entries[index] = _entries[index] with { Screen = null, HasUnknownPrefix = true };
        }
    }

    static string? Written(RouteNode node, string name) => node.Properties.TryGetValue(name, out var value) ? value.Written : null;

    static string? PathOf(RouteNode node, string parent)
    {
        if (node.Properties.TryGetValue("index", out var index))
        {
            return index.Literal is null && string.Equals(index.Written, "true", StringComparison.Ordinal) && !node.Properties.ContainsKey("path") && node.Children.Count == 0 ? parent : null;
        }

        if (!node.Properties.TryGetValue("path", out var written))
        {
            return parent;
        }

        return written.Literal is { } literal && !written.Written.StartsWith('`') ? RoutePaths.Join(parent, literal) : null;
    }

    void ReadNode(RouteNode node, string parent)
    {
        if (ReadablePath(node, parent) is not { } path)
        {
            return;
        }

        ReadElement(node, path);
        foreach (var child in node.Children)
        {
            ReadNode(child, path);
        }
    }

    string? ReadablePath(RouteNode node, string parent)
    {
        if (!node.IsReadable)
        {
            Omit("computed or spread route declarations or children");
            _entries.Add(new(parent, null, IncludesDescendants: true));
            return null;
        }

        var routePath = PathOf(node, parent);
        if (routePath is null)
        {
            Omit($"unsupported path/index '{Written(node, "path") ?? Written(node, "index")}'");
            _entries.Add(new(parent, null, IncludesDescendants: true));
            return null;
        }

        return HasDefaultSensitivity(node, routePath) ? routePath : null;
    }

    bool HasDefaultSensitivity(RouteNode node, string path)
    {
        if (!node.Properties.TryGetValue("caseSensitive", out var sensitivity) || (sensitivity.Literal is null && string.Equals(sensitivity.Written, "false", StringComparison.Ordinal)))
        {
            return true;
        }

        Omit($"non-default caseSensitive route '{path}'");
        _entries.Add(new(path, null, IncludesDescendants: node.Children.Count > 0));
        return false;
    }

    void ReadElement(RouteNode node, string path)
    {
        var expression = Written(node, "element");
        var file = expression is not null ? _elements.File(expression) : null;
        var screen = node.Properties.ContainsKey("lazy") ? null : _elements.Screen(file);
        var unread = node.Properties.ContainsKey("lazy") || (expression is not null && file is null);
        if (unread || screen is not null || node.Children.Count == 0)
        {
            _entries.Add(new(path, screen, IncludesDescendants: unread && node.Children.Count > 0));
        }

        ReportElement(node, path, expression, screen);
    }

    void ReportElement(RouteNode node, string path, string? expression, RouteScreen? screen)
    {
        if (node.Properties.ContainsKey("lazy"))
        {
            Omit($"lazy route '{path}'");
        }
        else if (screen is null && (node.Children.Count == 0 || (expression is not null && _elements.File(expression) is null)))
        {
            Omit($"unresolved or computed element '{expression ?? "<none>"}' at '{path}'");
        }
    }
}
