// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Finds context references in literal patterns; the language binder still owns pattern validity and support.
/// </summary>
internal static partial class GenerationPatternContext
{
    /// <summary>
    /// Detects date tokens without treating escaped text or character-class contents as context references.
    /// </summary>
    /// <param name="pattern">The authored literal pattern.</param>
    /// <returns>Whether the no-context Fundamentals overload cannot supply the declared dates.</returns>
    internal static bool UsesDates(string pattern) => Tokens().Matches(pattern).Cast<Match>().Any(match => match.Groups["date"].Success);

    [GeneratedRegex("""\\.| \[(?:\\.|[^\]\\])*\] | (?<date>\{(?:yyyy|yy|MM|dd)\})""", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex Tokens();
}
