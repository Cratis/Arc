// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// String extensions.
/// </summary>
public static class StringExtensions
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
}
