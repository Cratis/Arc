// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Validates the independent report destination and publishes JSON atomically.
/// </summary>
internal static class ValidationReportFile
{
    internal static bool TryValidate(string path, string? output, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(output))
        {
            error = "--validation-report-file requires a path and a proxy output path.";
            return false;
        }
        try
        {
            var destination = Path.GetFullPath(path);
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(output));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (destination.Equals(root, comparison) || destination.StartsWith(Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar, comparison))
                error = "--validation-report-file must be outside the proxy output tree.";
            else if (Directory.Exists(destination)) error = "--validation-report-file cannot be an existing directory.";
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = "--validation-report-file requires a valid path.";
        }
        return error.Length == 0;
    }

    internal static void Write(string path, string json)
    {
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporary, json);
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
