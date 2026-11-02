// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Provides assertions over accepted and rejected command decisions, without invoking assertion policies.
/// </summary>
public static class CommandRejectionShouldExtensions
{
    /// <summary>
    /// Asserts that the decision accepted a non-null result.
    /// </summary>
    /// <typeparam name="TResult">The accepted value type.</typeparam>
    /// <param name="result">The returned decision.</param>
    /// <returns>The accepted value.</returns>
    /// <exception cref="CommandDecisionAssertionException">The decision is null, rejected, or has a null value.</exception>
    public static TResult ShouldBeAccepted<TResult>(this Result<TResult, ValidationResult>? result)
    {
        if (result is null)
        {
            throw new CommandDecisionAssertionException("Expected command to be accepted, but the result was <null>.");
        }

        if (result.TryGetResult(out var value) && value is not null)
        {
            return value;
        }

        throw new CommandDecisionAssertionException(
            $"Expected command to be accepted, but it was {CommandDecisionValues.Describe(result.Value)}.");
    }

    /// <summary>
    /// Asserts that the decision was rejected with a non-null validation result.
    /// </summary>
    /// <typeparam name="TResult">The accepted value type.</typeparam>
    /// <param name="result">The returned decision.</param>
    /// <returns>The rejection.</returns>
    /// <exception cref="CommandDecisionAssertionException">The decision is null, accepted, or has a null rejection.</exception>
    public static ValidationResult ShouldBeRejected<TResult>(this Result<TResult, ValidationResult>? result)
    {
        if (result is null)
        {
            throw new CommandDecisionAssertionException("Expected command to be rejected, but the result was <null>.");
        }

        if (result.TryGetError(out var rejection) && rejection is not null)
        {
            return rejection;
        }

        throw new CommandDecisionAssertionException(
            $"Expected command to be rejected, but it was {CommandDecisionValues.Describe(result.Value)}.");
    }

    /// <summary>
    /// Asserts that the decision was rejected with the exact message, using ordinal comparison.
    /// </summary>
    /// <typeparam name="TResult">The accepted value type.</typeparam>
    /// <param name="result">The returned decision.</param>
    /// <param name="reason">The exact rejection message.</param>
    /// <returns>The rejection.</returns>
    /// <exception cref="CommandDecisionAssertionException">The decision was not rejected with that message.</exception>
    public static ValidationResult ShouldBeRejected<TResult>(this Result<TResult, ValidationResult>? result, string reason)
    {
        var rejection = result.ShouldBeRejected();
        if (!string.Equals(rejection.Message, reason, StringComparison.Ordinal))
        {
            throw new CommandDecisionAssertionException(
                $"Expected command to be rejected with '{reason}', but it was not. Actual: {CommandDecisionValues.Describe(rejection)}.");
        }

        return rejection;
    }

    /// <summary>
    /// Asserts that the decision was rejected for the specified machine-readable validation reason.
    /// </summary>
    /// <typeparam name="TResult">The accepted value type.</typeparam>
    /// <param name="result">The returned decision.</param>
    /// <param name="reason">The expected validation reason.</param>
    /// <returns>The rejection.</returns>
    /// <exception cref="CommandDecisionAssertionException">The decision was not rejected for that reason.</exception>
    public static ValidationResult ShouldHaveValidationErrorBecauseOf<TResult>(this Result<TResult, ValidationResult>? result, ValidationResultReason reason)
    {
        var rejection = result.ShouldBeRejected();
        if (rejection.Reason != reason)
        {
            throw new CommandDecisionAssertionException(
                $"Expected command to have a validation error with reason '{reason}', but it did not. Actual: {CommandDecisionValues.Describe(rejection)}.");
        }

        return rejection;
    }
}
