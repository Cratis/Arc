// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Reads lexical Route children of an imported Routes container, rejecting conditional or wrapped declarations.
/// </summary>
internal static class RouteJsxReader
{
    /// <summary>
    /// Gets the route nodes within actual Routes containers.
    /// </summary>
    /// <param name="source">The route module.</param>
    /// <param name="route">The local Route tag.</param>
    /// <param name="routes">The local Routes tag.</param>
    /// <returns>The nodes, or unread placeholders for unsupported tables.</returns>
    internal static IReadOnlyList<RouteNode> Read(TsxSource source, string route, string routes)
    {
        var tables = JsxElements.Named(source, routes).ToList();
        return tables.Count == 0 ? [RouteNode.Unread] : tables.SelectMany(table => Root(source, table, route)).ToList();
    }

    static List<RouteNode> Root(TsxSource source, JsxElement table, string route) => table["location"] is not null
        ? [RouteNode.Unread] : Children(source, table, route);

    static List<RouteNode> Children(TsxSource source, JsxElement parent, string route)
    {
        var index = source.Tags.ToList().FindIndex(_ => _.Start == parent.Position);
        var direct = source.Tags.Where(_ => _.Parent == index && _.Start >= parent.ChildrenStart && _.End <= parent.ChildrenEnd).ToList();
        if (!parent.IsReadable || source.Tags[index].HasContent || direct.Exists(_ => _.Name != route))
        {
            return [RouteNode.Unread];
        }

        return JsxElements.Named(parent, route).Select(element => Node(source, element, route)).ToList();
    }

    static RouteNode Node(TsxSource source, JsxElement element, string route) =>
        new(element.Attributes, Children(source, element, route), element.IsReadable);
}
