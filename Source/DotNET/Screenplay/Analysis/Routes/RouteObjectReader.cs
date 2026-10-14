// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Reads direct router factory calls whose first and only argument is a literal route array.
/// </summary>
internal static partial class RouteObjectReader
{
    /// <summary>
    /// Gets route nodes from calls to a proven imported router factory.
    /// </summary>
    /// <param name="source">The route table source.</param>
    /// <param name="factory">The local imported factory name.</param>
    /// <returns>The literal nodes or unread placeholders.</returns>
    internal static IEnumerable<RouteNode> Read(TsxSource source, string factory) => CallRegex().Matches(source.Code).Cast<Match>()
        .Where(_ => _.Groups["name"].Value == factory && source.IsCode(_.Index))
        .SelectMany(call => Call(source, call.Index + call.Length - 1));

    /// <summary>
    /// Determines whether a factory binding is actually called, rather than merely imported.
    /// </summary>
    /// <param name="source">The scanned module.</param>
    /// <param name="factory">The imported local factory name.</param>
    /// <returns>Whether the module calls this factory.</returns>
    internal static bool HasCalls(TsxSource source, string factory) => CallRegex().Matches(source.Code).Cast<Match>()
        .Any(_ => _.Groups["name"].Value == factory && source.IsCode(_.Index));

    static List<RouteNode> Call(TsxSource source, int opening)
    {
        var end = RouteLiteralParts.EndOf(source, opening);
        return end > opening ? Array(source.Code[(opening + 1)..(end - 1)]) ?? [RouteNode.Unread] : [RouteNode.Unread];
    }

    static List<RouteNode>? Array(string text) => RouteLiteralParts.Members(text, '[')?.Select(Object).ToList();

    static RouteNode Object(string text)
    {
        if (RouteLiteralParts.Fields(text) is not { } fields)
        {
            return RouteNode.Unread;
        }

        var children = fields.TryGetValue("children", out var written) ? Array(written) : [];
        var properties = fields.Where(_ => _.Key != "children").ToDictionary(_ => _.Key, _ => JsxValue.Expression(_.Value), StringComparer.Ordinal);
        return new(properties, children ?? [], children is not null);
    }

    [GeneratedRegex(@"\b(?<name>[A-Za-z_$][\w$]*)\s*\(", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CallRegex();
}
