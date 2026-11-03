// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.CompilerServices;
using Cratis.Arc.Validation;
using OneOf;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Normalizes and describes returned decisions without interpreting them as appended events.
/// </summary>
internal static class CommandDecisionValues
{
    /// <summary>
    /// Flattens active union values, tuples and collections in order.
    /// </summary>
    /// <param name="value">The returned decision.</param>
    /// <param name="includeNulls">Whether to retain null entries for failure diagnostics.</param>
    /// <returns>The normalized values, excluding null by default.</returns>
    internal static IEnumerable<object?> Flatten(object? value, bool includeNulls = false) =>
        Expand(value).Where(value => includeNulls || value is not null);

    /// <summary>
    /// Unwraps active union branches without expanding tuples or collections.
    /// </summary>
    /// <param name="value">The returned decision.</param>
    /// <returns>The active value.</returns>
    internal static object? Unwrap(object? value) => value is IOneOf union ? Unwrap(union.Value) : value;

    /// <summary>
    /// Describes a value, retaining useful record formatting.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <returns>A diagnostic description.</returns>
    internal static string Describe(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        if (value is ValidationResult rejection)
        {
            return Describe(rejection);
        }

        var type = value.GetType();
        var text = value.ToString();
        if (IsDefaultText(type, text))
        {
            return type.Name;
        }

        return text!.StartsWith(type.Name, StringComparison.Ordinal) ? text! : $"{type.Name} ({text})";
    }

    /// <summary>
    /// Describes the severity, reason, detail and message of a rejection.
    /// </summary>
    /// <param name="value">The rejection.</param>
    /// <returns>The rejection diagnostic.</returns>
    internal static string Describe(ValidationResult value) =>
        $"{value.Severity} {value.Reason}/{value.ReasonDetail ?? "<no detail>"} ('{value.Message}')";

    /// <summary>
    /// Describes all candidates, including nulls, with their normalized positions.
    /// </summary>
    /// <param name="values">The candidates.</param>
    /// <returns>An ordered diagnostic, or no values for an empty collection.</returns>
    internal static string DescribeAll(IEnumerable<object?> values)
    {
        var descriptions = values.SelectMany(Expand).Select((value, index) => $"[{index}] {Describe(value)}").ToArray();
        return descriptions.Length == 0 ? "no values" : string.Join(", ", descriptions);
    }

    /// <summary>
    /// Tests an exact or explicitly assignable type match, never matching null.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="value">The actual candidate.</param>
    /// <param name="assignable">Whether derived types are accepted.</param>
    /// <returns>Whether the candidate matches.</returns>
    internal static bool Matches<TEvent>(object? value, bool assignable) =>
        value is not null && (assignable ? value is TEvent : value.GetType() == typeof(TEvent));

    /// <summary>
    /// Suggests assignable matching when the requested exact type cannot be instantiated.
    /// </summary>
    /// <typeparam name="TEvent">The requested type.</typeparam>
    /// <param name="assignable">Whether assignable matching is already selected.</param>
    /// <param name="method">The exact matching method name.</param>
    /// <returns>The diagnostic hint, if applicable.</returns>
    internal static string AssignableHint<TEvent>(bool assignable, string method) =>
        !assignable && (typeof(TEvent).IsInterface || typeof(TEvent).IsAbstract)
            ? $"; use {method}AssignableTo<T> to accept derived types"
            : string.Empty;

    static bool IsDefaultText(Type type, string? text) => text is null || text == type.FullName || text == type.ToString();

    static IEnumerable<object?> Expand(object? value)
    {
        switch (value)
        {
            case IOneOf union:
                return Expand(union.Value);
            case ITuple tuple:
                return Enumerable.Range(0, tuple.Length).SelectMany(index => Expand(tuple[index]));
            case IEnumerable collection when value is not string:
                return collection.Cast<object?>().SelectMany(Expand);
            default:
                return [value];
        }
    }
}
