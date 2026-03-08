// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Combines multiple generated TypeScript file contents into a single file.
/// </summary>
public static class TypeScriptContentCombiner
{
    /// <summary>
    /// Combines multiple TypeScript file contents into a single file content.
    /// The DO NOT EDIT header is taken from the first content, preamble lines (ESLint directives
    /// and import statements) are deduplicated, and all export declarations are concatenated.
    /// </summary>
    /// <param name="contents">The TypeScript file contents to combine.</param>
    /// <returns>The combined TypeScript file content.</returns>
    public static string Combine(IEnumerable<string> contents)
    {
        var contentList = contents.ToList();

        if (contentList.Count == 0)
        {
            return string.Empty;
        }

        if (contentList.Count == 1)
        {
            return contentList[0];
        }

        var parsed = contentList.ConvertAll(ParseContent);

        var header = parsed[0].Header;

        var seenPreambleLines = new HashSet<string>(StringComparer.Ordinal);
        var eslintLines = new List<string>();
        var importLines = new List<string>();

        foreach (var section in parsed)
        {
            foreach (var line in section.PreambleLines)
            {
                if (!seenPreambleLines.Add(line))
                {
                    continue;
                }

                if (line.TrimStart().StartsWith("import ", StringComparison.Ordinal))
                {
                    importLines.Add(line);
                }
                else
                {
                    eslintLines.Add(line);
                }
            }
        }

        var bodies = parsed
            .Select(s => s.Body.Trim())
            .Where(b => !string.IsNullOrEmpty(b))
            .ToList();

        var sb = new StringBuilder()
            .AppendLine(header);

        foreach (var line in eslintLines)
        {
            sb.AppendLine(line);
        }

        foreach (var line in importLines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine()
          .AppendJoin($"{Environment.NewLine}{Environment.NewLine}", bodies)
          .AppendLine();

        return sb.ToString();
    }

    static ParsedContent ParseContent(string content)
    {
        var lines = content.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        var exportStartIndex = lines.FindIndex(l => l.StartsWith("export ", StringComparison.Ordinal));

        if (exportStartIndex < 0)
        {
            return new ParsedContent(string.Empty, [], content);
        }

        var header = string.Join('\n', lines.Take(3));

        // Walk backwards from the export to include any preceding JSDoc block in the body,
        // so that per-type documentation is not mixed into the shared preamble section.
        var bodyStartIndex = exportStartIndex;
        var checkIndex = exportStartIndex - 1;

        while (checkIndex >= 0 && string.IsNullOrWhiteSpace(lines[checkIndex]))
        {
            checkIndex--;
        }

        if (checkIndex >= 0 && lines[checkIndex].TrimStart().StartsWith("*/", StringComparison.Ordinal))
        {
            var scanIndex = checkIndex;
            while (scanIndex >= 0 && !lines[scanIndex].TrimStart().StartsWith("/**", StringComparison.Ordinal))
            {
                scanIndex--;
            }

            if (scanIndex >= 0 && lines[scanIndex].TrimStart().StartsWith("/**", StringComparison.Ordinal))
            {
                bodyStartIndex = scanIndex;
            }
        }

        var preambleLines = lines
            .Skip(3)
            .Take(bodyStartIndex - 3)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var body = string.Join('\n', lines.Skip(bodyStartIndex));

        return new ParsedContent(header, preambleLines, body);
    }

    sealed record ParsedContent(string Header, IReadOnlyList<string> PreambleLines, string Body);
}
