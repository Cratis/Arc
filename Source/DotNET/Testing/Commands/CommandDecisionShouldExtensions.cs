// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.CompilerServices;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Provides assertions over the decision returned by a command handler.
/// </summary>
/// <remarks>
/// The object receiver supports events, Result values, tuples and collections. These assertions neither execute
/// the pipeline nor prove an append, and never consult CommandResult assertion policies.
/// </remarks>
public static class CommandDecisionShouldExtensions
{
    /// <summary>
    /// Asserts that the decision is exactly the specified event type and satisfies the optional predicate.
    /// </summary>
    /// <typeparam name="TEvent">The exact event type.</typeparam>
    /// <param name="decision">The returned decision, optionally wrapped in a Result.</param>
    /// <param name="predicate">An optional condition on the event.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The matched event.</returns>
    /// <exception cref="CommandDecisionAssertionException">The decision does not match.</exception>
    public static TEvent ShouldBeEvent<TEvent>(this object? decision, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        AssertEvent(decision, predicate, predicateExpression, false);

    /// <summary>
    /// Asserts that the decision is assignable to the event type and satisfies the optional predicate.
    /// </summary>
    /// <typeparam name="TEvent">The event type or interface to accept.</typeparam>
    /// <param name="decision">The returned decision, optionally wrapped in a Result.</param>
    /// <param name="predicate">An optional condition on the event.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The matched event.</returns>
    /// <exception cref="CommandDecisionAssertionException">The decision does not match.</exception>
    public static TEvent ShouldBeEventAssignableTo<TEvent>(this object? decision, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        AssertEvent(decision, predicate, predicateExpression, true);

    /// <summary>
    /// Asserts that the normalized decision contains an event of exactly the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The exact event type.</typeparam>
    /// <param name="decision">The returned event, Result, tuple or collection.</param>
    /// <param name="predicate">An optional condition on the event.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The first matching event in normalized order.</returns>
    /// <exception cref="CommandDecisionAssertionException">No event matches.</exception>
    public static TEvent ShouldContainEvent<TEvent>(this object? decision, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        AssertContains(decision, predicate, predicateExpression, false);

    /// <summary>
    /// Asserts that the normalized decision contains an event assignable to the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type or interface to accept.</typeparam>
    /// <param name="decision">The returned event, Result, tuple or collection.</param>
    /// <param name="predicate">An optional condition on the event.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The first matching event in normalized order.</returns>
    /// <exception cref="CommandDecisionAssertionException">No event matches.</exception>
    public static TEvent ShouldContainEventAssignableTo<TEvent>(this object? decision, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        AssertContains(decision, predicate, predicateExpression, true);

    static TEvent AssertEvent<TEvent>(object? decision, Func<TEvent, bool>? predicate, string expression, bool assignable)
    {
        var value = CommandDecisionValues.Unwrap(decision);
        if (IsSingleEvent<TEvent>(value, assignable) && SatisfiesPredicate((TEvent)value!, predicate))
        {
            return (TEvent)value!;
        }

        var actual = value is ValidationResult rejection
            ? $"it was rejected: {CommandDecisionValues.Describe(rejection)}"
            : $"it was {CommandDecisionValues.Describe(value)}";
        var hint = IsCollection(value) ? "; use ShouldContainEvent" : string.Empty;
        throw new CommandDecisionAssertionException(
            $"Expected command to return event '{typeof(TEvent).Name}' matching '{expression}', but {actual}{hint}{CommandDecisionValues.AssignableHint<TEvent>(assignable, "ShouldBeEvent")}.");
    }

    static bool IsCollection(object? value) => value is ITuple || (value is IEnumerable && value is not string);

    static bool IsSingleEvent<TEvent>(object? value, bool assignable) =>
        !IsCollection(value) && CommandDecisionValues.Matches<TEvent>(value, assignable);

    static bool SatisfiesPredicate<TEvent>(TEvent value, Func<TEvent, bool>? predicate) => predicate is null || predicate(value);

    static TEvent AssertContains<TEvent>(object? decision, Func<TEvent, bool>? predicate, string expression, bool assignable)
    {
        var values = CommandDecisionValues.Flatten(decision, includeNulls: true).ToArray();
        var matches = values.Where(value => CommandDecisionValues.Matches<TEvent>(value, assignable)).Cast<TEvent>();
        foreach (var match in matches.Where(value => predicate is null || predicate(value)))
        {
            return match;
        }

        throw new CommandDecisionAssertionException(
            $"Expected command to contain event '{typeof(TEvent).Name}' matching '{expression}', but none matched. Actual: {CommandDecisionValues.DescribeAll(values)}{CommandDecisionValues.AssignableHint<TEvent>(assignable, "ShouldContainEvent")}.");
    }
}
