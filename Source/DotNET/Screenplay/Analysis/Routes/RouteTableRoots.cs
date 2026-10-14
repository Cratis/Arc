// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Selects literal JSX or router-factory roots using the table's imported router identities.
/// </summary>
internal static class RouteTableRoots
{
    /// <summary>
    /// Reads table roots without confusing package names with local aliases.
    /// </summary>
    /// <param name="source">The route module.</param>
    /// <param name="imports">The proven import bindings.</param>
    /// <param name="omit">The per-table omission reporter.</param>
    /// <returns>The root declarations.</returns>
    internal static IEnumerable<RouteNode> Read(TsxSource source, IReadOnlyDictionary<string, ScreenImportBinding> imports, Action<string> omit)
    {
        var router = imports.Values.Where(RouteTable.IsRouter).ToList();
        return Jsx(source, router, omit).Concat(router.Where(_ => _.Name == "createBrowserRouter")
            .SelectMany(_ => RouteObjectReader.Read(source, _.Local)));
    }

    static IReadOnlyList<RouteNode> Jsx(TsxSource source, List<ScreenImportBinding> router, Action<string> omit)
    {
        var route = router.Where(_ => _.Name == "Route").ToList();
        var routes = router.Where(_ => _.Name == "Routes").ToList();
        if (route.Count == 1 && routes.Count == 1)
        {
            return RouteJsxReader.Read(source, route[0].Local, routes[0].Local, omit);
        }

        if (route.Count + routes.Count > 0)
        {
            omit("Route/Routes imports without one literal Routes container binding");
            return [RouteNode.Unread];
        }

        return [];
    }
}
