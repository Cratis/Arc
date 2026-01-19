// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc;

/// <summary>
/// String extensions.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Converts a string to kebab-case.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The kebab-case version of the input string.</returns>
    public static string ToKebabCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new System.Text.StringBuilder();

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            // Replace underscores with dashes
            if (c == '_')
            {
                result.Append('-');
                continue;
            }

            // If uppercase and not the first character, add a dash before it
            // But not if the previous character was an underscore (already added a dash)
            if (char.IsUpper(c) && i > 0 && input[i - 1] != '_')
            {
                result.Append('-');
            }

            result.Append(char.ToLowerInvariant(c));
        }

        return result.ToString();
    }

    /// <summary>
    /// Sanitizes a URL by removing invalid patterns such as double slashes,
    /// trailing slashes, and other malformed URL segments.
    /// </summary>
    /// <param name="url">The URL to sanitize.</param>
    /// <returns>The sanitized URL.</returns>
#pragma warning disable CA1055 // URI-like return values should not be strings - this is a string extension for URL path manipulation
    public static string SanitizeUrl(this string url)
#pragma warning restore CA1055
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        // Replace multiple consecutive slashes with a single slash (except for protocol://)
        url = MultipleSlashesRegex().Replace(url, "/");

        // Remove trailing slashes
        url = url.TrimEnd('/');

        // Ensure the URL starts with a single slash if it's a relative URL
        if (!url.StartsWith('/') && !url.Contains("://"))
        {
            url = "/" + url;
        }

        return url;
    }

    [GeneratedRegex(@"(?<!:)/{2,}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MultipleSlashesRegex();
}
