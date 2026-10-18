// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Reads header templates from project-wide C# EditorConfig sections, without a glob dependency.
/// </summary>
internal static class EditorConfigFileHeader
{
    /// <summary>
    /// Finds the nearest applicable header template without crossing an EditorConfig root.
    /// </summary>
    /// <param name="projectDirectory">The C# project directory.</param>
    /// <returns>The inherited template, or an empty string if disabled or absent.</returns>
    internal static string Resolve(string projectDirectory)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(projectDirectory)); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, ".editorconfig");
            if (!File.Exists(path))
            {
                continue;
            }

            var (template, root) = Read(path);
            if (template is not null)
            {
                return template.Equals("unset", StringComparison.OrdinalIgnoreCase) ? string.Empty : template;
            }
            if (root)
            {
                break;
            }
        }

        return string.Empty;
    }

    static (string? Template, bool Root) Read(string path)
    {
        string? template = null;
        var root = false;
        var inSection = false;
        var applicable = false;
        foreach (var line in File.ReadLines(path).Select(line => line.Trim()))
        {
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                inSection = true;
                applicable = IsApplicable(line[1..^1]);
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (!inSection && key.Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                root = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            if (applicable && key.Equals("file_header_template", StringComparison.OrdinalIgnoreCase))
            {
                template = value;
            }
        }

        return (template, root);
    }

    static bool IsApplicable(string section) => section.Equals("*", StringComparison.Ordinal) || section.Equals("*.cs", StringComparison.Ordinal) ||
        (section.StartsWith("*.{", StringComparison.Ordinal) && section.EndsWith('}') &&
         section[3..^1].Split(',').Contains("cs", StringComparer.Ordinal));
}
