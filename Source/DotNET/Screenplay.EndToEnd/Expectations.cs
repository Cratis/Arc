// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Holds what a generated document, and what was reported while generating it, have to say.
/// </summary>
/// <param name="expectations">The expectations to hold a generation to.</param>
/// <remarks>
/// Reading a document back through the compiler proves it is valid, which is not the same as proving it is true. A
/// generator that quietly declined to read something produces a document that is smaller and still compiles, so the
/// check that catches it has to be about what the document says rather than about whether it came back clean.
/// <para>
/// Diagnostics are held to alongside the document because the two halves of that are not both visible in the text. A
/// value the generator recovered and a value it gave up on are both absent from the document - the first because
/// there was nothing left to say and the second because nothing could be said - and only the report tells them apart.
/// </para>
/// </remarks>
public class Expectations(IReadOnlyList<Expectation> expectations)
{
    /// <summary>
    /// Reads the expectations declared in a file.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    /// <returns>The <see cref="Expectations"/>.</returns>
    public static async Task<Expectations> In(string path) =>
        new([.. (await File.ReadAllLinesAsync(path)).Select(Expectation.Read).OfType<Expectation>()]);

    /// <summary>
    /// Gets everything a generation failed to hold to.
    /// </summary>
    /// <param name="document">The generated document.</param>
    /// <param name="diagnostics">What was reported while generating it.</param>
    /// <returns>The failures, empty when the generation held to all of them.</returns>
    public IEnumerable<string> NotMetBy(string document, IEnumerable<ScreenplayDiagnostic> diagnostics)
    {
        var lines = document.Split('\n').Select(_ => _.Trim()).ToList();
        var codes = diagnostics.Select(_ => _.Code).ToList();

        return [.. expectations.Select(_ => NotMetBy(_, lines, codes)).OfType<string>()];
    }

    /// <summary>
    /// Gets how one expectation was not held to.
    /// </summary>
    /// <param name="expectation">The expectation to check.</param>
    /// <param name="lines">The lines of the document, trimmed.</param>
    /// <param name="codes">The code of everything reported.</param>
    /// <returns>The failure, or <see langword="null"/> when it was held to.</returns>
    static string? NotMetBy(Expectation expectation, IReadOnlyList<string> lines, IReadOnlyList<string> codes)
    {
        var found = expectation.Kind == ExpectationKind.Says
            ? lines.Count(_ => string.Equals(_, expectation.Subject, StringComparison.Ordinal))
            : codes.Count(_ => string.Equals(_, expectation.Subject, StringComparison.Ordinal));

        if (expectation.Count is { } expected)
        {
            return found == expected
                ? null
                : Describe(expectation, $"{expected} time(s)", found);
        }

        return found > 0 ? null : Describe(expectation, "at least once", found);
    }

    /// <summary>
    /// Describes an expectation that was not held to.
    /// </summary>
    /// <param name="expectation">The expectation.</param>
    /// <param name="expected">How often it was expected.</param>
    /// <param name="found">How often it occurred.</param>
    /// <returns>The description.</returns>
    static string Describe(Expectation expectation, string expected, int found) =>
        expectation.Kind == ExpectationKind.Says
            ? $"the document was to say '{expectation.Subject}' {expected}, and says it {found} time(s)"
            : $"'{expectation.Subject}' was to be reported {expected}, and was reported {found} time(s)";
}
