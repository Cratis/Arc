// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Represents one thing a generated document, or what was reported while generating it, has to hold.
/// </summary>
/// <param name="Kind">What is being held to.</param>
/// <param name="Subject">The line of the document, or the code of the diagnostic.</param>
/// <param name="Count">How many times it has to occur, or <see langword="null"/> for at least once.</param>
public record Expectation(ExpectationKind Kind, string Subject, int? Count)
{
    /// <summary>
    /// Reads an expectation from the line declaring it.
    /// </summary>
    /// <param name="line">The line to read.</param>
    /// <returns>The <see cref="Expectation"/>, or <see langword="null"/> when the line declares none.</returns>
    /// <exception cref="UnreadableExpectation">Thrown when the line names no directive, or a count that is not one.</exception>
    /// <remarks>
    /// A line that cannot be read is an error rather than an expectation quietly skipped. A check whose expectations
    /// file has a typo in it passes everything, which is the one outcome worse than failing.
    /// </remarks>
    public static Expectation? Read(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return null;
        }

        var separator = trimmed.IndexOf(' ', StringComparison.Ordinal);
        var directive = separator < 0 ? trimmed : trimmed[..separator];
        var rest = separator < 0 ? string.Empty : trimmed[(separator + 1)..].Trim();

        return directive switch
        {
            "says" => new(ExpectationKind.Says, Stated(rest, line), null),
            "once" => new(ExpectationKind.Says, Stated(rest, line), 1),
            "never" => new(ExpectationKind.Says, Stated(rest, line), 0),
            "reports" => Reported(rest, line),
            _ => throw new UnreadableExpectation(line)
        };
    }

    /// <summary>
    /// Reads the expectation a <c>reports</c> line declares.
    /// </summary>
    /// <param name="rest">What follows the directive.</param>
    /// <param name="line">The whole line, for use in the error.</param>
    /// <returns>The <see cref="Expectation"/>.</returns>
    /// <exception cref="UnreadableExpectation">Thrown when the line names no code and count.</exception>
    static Expectation Reported(string rest, string line)
    {
        var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length == 2 && int.TryParse(parts[1], out var count) && count >= 0
            ? new(ExpectationKind.Reports, parts[0], count)
            : throw new UnreadableExpectation(line);
    }

    /// <summary>
    /// Gets what a directive states, refusing an empty one.
    /// </summary>
    /// <param name="rest">What follows the directive.</param>
    /// <param name="line">The whole line, for use in the error.</param>
    /// <returns>What was stated.</returns>
    /// <exception cref="UnreadableExpectation">Thrown when the directive states nothing.</exception>
    static string Stated(string rest, string line) =>
        rest.Length > 0 ? rest : throw new UnreadableExpectation(line);
}
