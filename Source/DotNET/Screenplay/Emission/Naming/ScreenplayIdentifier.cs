// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Emission.Naming;

/// <summary>
/// Answers whether a value can be written to the document as a bare word rather than as a quoted string.
/// </summary>
public static partial class ScreenplayIdentifier
{
    /// <summary>
    /// The prefix marking a value as a key into the companion strings file rather than as literal text.
    /// </summary>
    public const string LocalizationPrefix = "$strings.";

    /// <summary>
    /// Determines whether a value is a bare identifier.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True when the value can be written without quotes.</returns>
    public static bool IsBareIdentifier(string? value) => !string.IsNullOrEmpty(value) && BareIdentifier().IsMatch(value);

    /// <summary>
    /// Determines whether a message is a localization key rather than literal text.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True when the message is a localization key.</returns>
    public static bool IsLocalizationKey(string? message) =>
        message?.StartsWith(LocalizationPrefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// Gets the pattern a bare identifier has to match.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex BareIdentifier();
}
