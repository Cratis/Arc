// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Builds the property expressions a projection maps with.
/// </summary>
/// <remarks>
/// These are the expressions the emission half already knows how to convert, so analysis writes the same mini
/// language whichever shape the projection was declared in. That is what lets a fluent projection and a model-bound
/// one produce the same document when they say the same thing.
/// </remarks>
public static class ProjectionExpressions
{
    /// <summary>The expression yielding the identifier of the event source an event belongs to.</summary>
    public const string EventSourceId = "$eventSourceId";

    /// <summary>The expression yielding the identity that caused an event.</summary>
    public const string CausedBy = "$causedBy";

    /// <summary>The expression incrementing a read model property.</summary>
    public const string Increment = "$increment";

    /// <summary>The expression decrementing a read model property.</summary>
    public const string Decrement = "$decrement";

    /// <summary>The expression counting occurrences into a read model property.</summary>
    public const string Count = "$count";

    /// <summary>
    /// Builds the expression yielding a value from the event context.
    /// </summary>
    /// <param name="path">The path within the context.</param>
    /// <returns>The expression.</returns>
    public static string EventContext(string path) => $"$eventContext({path})";

    /// <summary>
    /// Builds the expression yielding a constant value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The expression.</returns>
    public static string Value(object? value) => $"$value({Format(value)})";

    /// <summary>
    /// Builds the expression adding an event property to a read model property.
    /// </summary>
    /// <param name="property">The event property.</param>
    /// <returns>The expression.</returns>
    public static string Add(string property) => $"$add({property})";

    /// <summary>
    /// Builds the expression subtracting an event property from a read model property.
    /// </summary>
    /// <param name="property">The event property.</param>
    /// <returns>The expression.</returns>
    public static string Subtract(string property) => $"$subtract({property})";

    /// <summary>
    /// Builds the expression identifying a read model by several properties at once.
    /// </summary>
    /// <param name="type">The name of the type the key identifies.</param>
    /// <param name="parts">The property to expression pairs making up the key.</param>
    /// <returns>The expression.</returns>
    public static string Composite(string type, IEnumerable<KeyValuePair<string, string>> parts) =>
        $"$composite({type}, {string.Join(", ", parts.Select(_ => $"{_.Key}={_.Value}"))})";

    /// <summary>
    /// Formats a constant the way the mini language reads it back.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted value.</returns>
    static string Format(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
