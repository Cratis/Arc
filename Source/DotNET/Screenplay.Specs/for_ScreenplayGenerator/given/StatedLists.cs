// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Reads the collections a generated document states for a property, as complete ordered item lists.
/// </summary>
/// <remarks>
/// A collection is stated either as <c>property none</c> or as <c>property</c> followed by one indented
/// <c>item</c> line per element. Every line indented under the property belongs to its block, so an extra trailing
/// element shows up in the list and makes it differ from the expected one.
/// </remarks>
public static class StatedLists
{
    /// <summary>
    /// Gets every collection stated for a property, in document order.
    /// </summary>
    /// <param name="source">The generated document source.</param>
    /// <param name="property">The property name.</param>
    /// <returns>One list of item values per stated collection, each value as written after <c>item</c>.</returns>
    public static IReadOnlyList<IReadOnlyList<string>> Of(string source, string property)
    {
        var lines = source.ReplaceLineEndings("\n").Split('\n');
        var lists = new List<IReadOnlyList<string>>();
        for (var index = 0; index < lines.Length; index++)
        {
            var trimmed = lines[index].Trim();
            if (trimmed == $"{property} none")
            {
                lists.Add([]);
                continue;
            }

            if (trimmed != property)
            {
                continue;
            }

            var indentation = IndentationOf(lines[index]);
            var items = new List<string>();
            while (index + 1 < lines.Length && lines[index + 1].Trim().Length > 0 && IndentationOf(lines[index + 1]) > indentation)
            {
                index++;
                var line = lines[index].Trim();
                items.Add(line.StartsWith("item ", StringComparison.Ordinal) ? line["item ".Length..] : line);
            }

            lists.Add(items);
        }

        return lists;
    }

    /// <summary>
    /// Formats string items the way a document states them, quoted.
    /// </summary>
    /// <param name="items">The expected string items.</param>
    /// <returns>The quoted items.</returns>
    public static string[] Quoted(params string[] items) => [.. items.Select(item => $"\"{item}\"")];

    static int IndentationOf(string line) => line.Length - line.TrimStart().Length;
}
