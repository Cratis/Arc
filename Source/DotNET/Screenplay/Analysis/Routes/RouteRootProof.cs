// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Proves root ownership from lexical router containers and factory calls, never from module dependencies.
/// </summary>
internal static class RouteRootProof
{
    static readonly HashSet<string> _factories = new(StringComparer.Ordinal) { "createBrowserRouter", "createHashRouter", "createMemoryRouter" };
    static readonly HashSet<string> _providers = new(StringComparer.Ordinal) { "BrowserRouter", "HashRouter", "MemoryRouter", "RouterProvider" };
    static readonly HashSet<string> _routeTags = new(StringComparer.Ordinal) { "Route", "Routes" };

    /// <summary>
    /// Identifies a router factory by its imported identity.
    /// </summary>
    /// <param name="binding">The import binding.</param>
    /// <returns>Whether the binding creates an independent router.</returns>
    internal static bool IsFactory(ScreenImportBinding binding) => IsRouterPackage(binding) &&
        _factories.Contains(binding.Name);

    /// <summary>
    /// Identifies a router package without inspecting application imports or their graph.
    /// </summary>
    /// <param name="binding">The import binding.</param>
    /// <returns>Whether the binding belongs to react-router.</returns>
    internal static bool IsRouterPackage(ScreenImportBinding binding) => string.Equals(binding.Module, "react-router", StringComparison.Ordinal) ||
        string.Equals(binding.Module, "react-router-dom", StringComparison.Ordinal);

    /// <summary>
    /// Distinguishes files declaring routes from entry points that only render a router around an imported component.
    /// </summary>
    /// <param name="source">The scanned module.</param>
    /// <param name="imports">Its import bindings.</param>
    /// <returns>Whether the module contains route declarations or unread potential declarations.</returns>
    internal static bool HasDeclarations(TsxSource source, IReadOnlyDictionary<string, ScreenImportBinding> imports)
    {
        var router = imports.Values.Where(RouteTable.IsRouter).ToList();
        var tags = router.Where(_ => _routeTags.Contains(_.Name)).Select(_ => _.Local).ToHashSet(StringComparer.Ordinal);
        return source.Problem is not null || source.Tags.Any(_ => tags.Contains(_.Name)) ||
            router.Exists(_ => IsFactory(_) && RouteObjectReader.HasCalls(source, _.Local));
    }

    /// <summary>
    /// Proves all JSX tables are inside router children, or that a route-only factory module creates its own router.
    /// </summary>
    /// <param name="source">The scanned module.</param>
    /// <param name="imports">Its import bindings.</param>
    /// <returns>Whether the module's table roots are lexically proven.</returns>
    internal static bool IsExplicitRoot(TsxSource source, IReadOnlyDictionary<string, ScreenImportBinding> imports)
    {
        var router = imports.Values.Where(IsRouterPackage).ToList();
        var routes = router.Where(_ => _.Name == "Routes").Select(_ => _.Local).ToHashSet(StringComparer.Ordinal);
        var tables = source.Tags.Where(_ => routes.Contains(_.Name)).ToList();
        var providers = router.Where(_ => _providers.Contains(_.Name))
            .Select(_ => _.Local).ToHashSet(StringComparer.Ordinal);
        return source.Problem is null && (tables.Count > 0
            ? tables.TrueForAll(table => HasRouterAncestor(source, table, providers))
            : router.Exists(_ => IsFactory(_) && RouteObjectReader.HasCalls(source, _.Local)));
    }

    static bool HasRouterAncestor(TsxSource source, JsxTag table, HashSet<string> providers)
    {
        for (var parent = table.Parent; parent >= 0; parent = source.Tags[parent].Parent)
        {
            var tag = source.Tags[parent];
            if (providers.Contains(tag.Name) && tag.IsComplete && table.Start >= tag.OpeningEnd && table.End <= tag.ChildrenEnd)
            {
                return true;
            }
        }

        return false;
    }
}
