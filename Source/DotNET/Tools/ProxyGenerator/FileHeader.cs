// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Resolves and formats the application's generated-file header.
/// </summary>
internal static class FileHeader
{
    /// <summary>
    /// The generated-file notice separating the application header from tracking metadata.
    /// </summary>
    internal const string GeneratedNotice = "// **DO NOT EDIT** - This file is an automatically generated file.";

    /// <summary>
    /// Selects the explicit template, EditorConfig fallback, or no header.
    /// </summary>
    /// <param name="projectDirectory">The C# project directory, if known.</param>
    /// <param name="configuredHeader">The MSBuild header value; none disables the fallback.</param>
    /// <returns>The selected template, or an empty string.</returns>
    internal static string Resolve(string? projectDirectory, string? configuredHeader)
    {
        if (!string.IsNullOrEmpty(configuredHeader))
        {
            return configuredHeader.Equals("none", StringComparison.OrdinalIgnoreCase) ? string.Empty : configuredHeader;
        }

        return projectDirectory is null ? string.Empty : EditorConfigFileHeader.Resolve(projectDirectory);
    }

    /// <summary>
    /// Formats a template as TypeScript line comments for one output file.
    /// </summary>
    /// <param name="template">The selected template.</param>
    /// <param name="filePath">The output file path.</param>
    /// <returns>The formatted header and a separating blank line, or an empty string.</returns>
    internal static string Format(string template, string filePath)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var normalized = template.Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var fileName = Path.GetFileName(filePath);
        var lines = normalized.Split('\n').Select(line => $"// {line.Replace("{fileName}", fileName, StringComparison.Ordinal)}");

        return $"{string.Join('\n', lines)}\n\n";
    }
}
