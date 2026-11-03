// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Xml.Linq;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Looks up XML documentation summaries, indexing each documentation file once.
/// </summary>
static class XmlDocumentationSummaries
{
    static readonly char[] _lineSeparators = ['\n', '\r'];
    static readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<string, string>>> _indexes = new();

    /// <summary>
    /// Gets the normalized summary for a member.
    /// </summary>
    /// <param name="xmlFile">Path to the XML documentation file.</param>
    /// <param name="memberName">The XML documentation member name, e.g. <c>T:Namespace.Type</c>.</param>
    /// <returns>The summary, or empty when the file or member is missing or has no summary.</returns>
    public static string Get(string xmlFile, string memberName)
    {
        var index = _indexes.GetOrAdd(xmlFile, file => new Lazy<IReadOnlyDictionary<string, string>>(() => BuildIndex(file))).Value;
        return index.TryGetValue(memberName, out var summary) ? summary : string.Empty;
    }

    static Dictionary<string, string> BuildIndex(string xmlFile)
    {
        var index = new Dictionary<string, string>();
        if (!File.Exists(xmlFile))
        {
            return index;
        }

        foreach (var member in XDocument.Load(xmlFile).Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            if (name is not null)
            {
                // First member with a given name wins, matching a linear first-match scan.
                index.TryAdd(name, Normalize(member.Element("summary")));
            }
        }

        return index;
    }

    static string Normalize(XElement? summary) =>
        summary is null
            ? string.Empty
            : string.Join(
                ' ',
                summary.Value
                    .Split(_lineSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s)));
}
