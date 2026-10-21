// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Resolves literal absolute gestures against the route tables and retained screens of the entire application.
/// </summary>
internal sealed class RouteCatalog
{
    readonly List<RouteEntry> _entries = [];

    /// <summary>
    /// Finds and reads route tables below source roots, reporting unsupported declarations once per table.
    /// </summary>
    /// <param name="files">The user interface file source.</param>
    /// <param name="roots">The common source roots of the compilations.</param>
    /// <param name="screens">The retained screen identities.</param>
    /// <param name="diagnostics">The omission reporter.</param>
    internal void Read(IUserInterfaceFiles files, IEnumerable<string> roots, IReadOnlyList<RouteScreen> screens, ScreenplayDiagnostics diagnostics)
    {
        var contents = roots.Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct(StringComparer.Ordinal)
            .SelectMany(files.Under).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .ToDictionary(path => path, files.Contents, StringComparer.Ordinal);
        var tables = contents.Select(file => Table(file.Key, file.Value, screens)).OfType<RouteTable>().ToList();
        foreach (var table in tables)
        {
            table.Read();
        }

        RetainClaims(tables);
        ReportDuplicates(tables);
        foreach (var table in tables.Where(_ => _.Issues.Any()))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ScreenStructureNotInferred,
                $"The route table '{table.Path}' was not fully read: {string.Join("; ", table.Issues)}; no navigation is inferred from those declarations",
                table.Path);
        }
    }

    /// <summary>
    /// Resolves an absolute literal navigation target and binds whole path parameters.
    /// </summary>
    /// <param name="namespace">The navigating screen's slice.</param>
    /// <param name="literal">The literal gesture destination.</param>
    /// <returns>The navigation model, or null when the route or its destination is ambiguous.</returns>
    internal ScreenNavigationModel? Resolve(string @namespace, string literal)
    {
        var matches = _entries.Where(entry => RoutePaths.CouldMatch(entry, literal)).ToList();
        if (matches.Count != 1 || matches[0].Screen is not { } screen || matches[0].Path is not { } pattern ||
            RoutePaths.Match(pattern, literal) is not { } parameters)
        {
            return null;
        }

        var name = screen.Namespace == @namespace ? screen.Name : $"{screen.Namespace}.{screen.Name}";
        return new(name, pattern, parameters);
    }

    static RouteTable? Table(string path, string? text, IReadOnlyList<RouteScreen> screens)
    {
        if (text?.Contains("react-router", StringComparison.Ordinal) is not true)
        {
            return null;
        }

        var source = TsxSource.Scan(text);
        var imports = ScreenImports.Bindings(source);
        return imports.Values.Any(RouteTable.IsRouter) && RouteRootProof.HasDeclarations(source, imports) ? new(path, source, imports, screens) : null;
    }

    void RetainClaims(List<RouteTable> tables)
    {
        foreach (var table in tables)
        {
            if (tables.Count > 1 && !table.IsExplicitRoot)
            {
                table.UnknownMount("route table without a lexical router root has an unknown parent prefix");
            }

            _entries.AddRange(table.Entries);
        }
    }

    void ReportDuplicates(List<RouteTable> tables)
    {
        var repeated = _entries.Where(_ => _.Path is not null && !_.IncludesDescendants && !_.HasUnknownPrefix)
            .GroupBy(_ => _.Path, StringComparer.OrdinalIgnoreCase).Where(_ => _.Count() > 1)
            .Select(_ => _.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            foreach (var entry in table.Entries.Where(_ => _.Path is not null && !_.HasUnknownPrefix && repeated.Contains(_.Path)))
            {
                table.Omit($"duplicate path '{entry.Path}'");
            }
        }
    }
}
