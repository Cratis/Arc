// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reports unsupported navigation even when it sits inside a recognized component's otherwise accounted span.
/// </summary>
internal static class UnreadNavigationGestures
{
    /// <summary>
    /// Reports Link targets, inline click callbacks and row-click callbacks without interpreting them as toolbar gestures.
    /// </summary>
    /// <param name="source">The screen source.</param>
    /// <param name="components">The import identities.</param>
    /// <param name="callbacks">The router binding recognizer.</param>
    /// <param name="unread">The omission reporter.</param>
    internal static void Report(TsxSource source, KnownComponents components, MenuCallbacks callbacks, UnreadScreenStructure unread)
    {
        var links = components.Imports.Values.Where(_ => _.Name == "Link" && (string.Equals(_.Module, "react-router", StringComparison.Ordinal) || string.Equals(_.Module, "react-router-dom", StringComparison.Ordinal)))
            .Select(_ => _.Local).ToHashSet(StringComparer.Ordinal);
        var elements = source.Tags.Select(_ => _.Name).Distinct(StringComparer.Ordinal)
            .SelectMany(tag => JsxElements.Named(source, tag));
        foreach (var element in elements)
        {
            if (links.Contains(element.Tag) && element["to"] is not null)
            {
                unread.NavigationGesture($"a <{element.Tag}> to target '{element["to"]!.Written}'");
            }

            ReportCallbacks(element, callbacks, unread);
        }
    }

    static void ReportCallbacks(JsxElement element, MenuCallbacks callbacks, UnreadScreenStructure unread)
    {
        foreach (var name in new[] { "onClick", "onRowClick", "rowClick" })
        {
            if (element[name] is { Literal: null } callback && callbacks.HasNavigationCall(callback.Written))
            {
                unread.NavigationGesture($"a <{element.Tag}> {name} navigation callback '{callback.Written}'");
            }
        }
    }
}
