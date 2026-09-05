// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the description an artifact carries in its documentation comment.
/// </summary>
/// <remarks>
/// A summary is the closest thing the source has to the description a Screenplay declaration takes, and it is the
/// one place a developer has already written down what an artifact is for.
/// </remarks>
public static class Documentation
{
    /// <summary>
    /// Gets the summary of a symbol as a single line of text.
    /// </summary>
    /// <param name="symbol">The symbol to read.</param>
    /// <returns>The summary, or <see langword="null"/> when the symbol carries none.</returns>
    public static string? SummaryOf(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml(preferredCulture: null, expandIncludes: false);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        var summary = TryParse(xml)?.Element("summary")?.Value;

        return Flatten(summary);
    }

    /// <summary>
    /// Parses a documentation comment, ignoring one that is not well formed.
    /// </summary>
    /// <param name="xml">The documentation comment.</param>
    /// <returns>The parsed element, or <see langword="null"/>.</returns>
    static XElement? TryParse(string xml)
    {
        try
        {
            return XElement.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reduces a summary to a single line, since a description is printed on one.
    /// </summary>
    /// <param name="summary">The summary to reduce.</param>
    /// <returns>The single line, or <see langword="null"/> when there is nothing left.</returns>
    static string? Flatten(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var words = summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var flattened = string.Join(' ', words);

        return flattened.Length == 0 ? null : flattened;
    }
}
