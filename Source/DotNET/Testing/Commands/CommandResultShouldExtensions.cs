// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Provides assertion extension methods for <see cref="CommandResult"/>.
/// </summary>
public static class CommandResultShouldExtensions
{
    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> represents a successful execution.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result is not successful.</exception>
    public static void ShouldBeSuccessful(this CommandResult result)
    {
        if (!result.IsSuccess)
        {
            var reasons = BuildFailureReasons(result);
            throw new CommandResultAssertionException($"Expected command to be successful, but it was not.{reasons}");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> does not represent a successful execution.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result is successful.</exception>
    public static void ShouldNotBeSuccessful(this CommandResult result)
    {
        if (result.IsSuccess)
        {
            throw new CommandResultAssertionException("Expected command to not be successful, but it was.");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> has a valid state (no validation errors).
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result has validation errors.</exception>
    public static void ShouldBeValid(this CommandResult result)
    {
        if (!result.IsValid)
        {
            var errors = string.Join(", ", result.ValidationResults.Select(v => v.Message));
            throw new CommandResultAssertionException($"Expected command to be valid, but it had validation errors: {errors}");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> has validation errors.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result has no validation errors.</exception>
    public static void ShouldHaveValidationErrors(this CommandResult result)
    {
        if (result.IsValid)
        {
            throw new CommandResultAssertionException("Expected command to have validation errors, but it had none.");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> has at least one validation error containing the given message.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <param name="message">The expected validation message fragment.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when no matching validation error is found.</exception>
    public static void ShouldHaveValidationErrorFor(this CommandResult result, string message)
    {
        if (!result.ValidationResults.Any(v => v.Message.Contains(message, StringComparison.Ordinal)))
        {
            var errors = string.Join(", ", result.ValidationResults.Select(v => v.Message));
            throw new CommandResultAssertionException(
                $"Expected command to have a validation error containing '{message}', but no matching error was found. Actual errors: {errors}");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> is authorized.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result is not authorized.</exception>
    public static void ShouldBeAuthorized(this CommandResult result)
    {
        if (!result.IsAuthorized)
        {
            throw new CommandResultAssertionException($"Expected command to be authorized, but it was not. Reason: {result.AuthorizationFailureReason}");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> is not authorized.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result is authorized.</exception>
    public static void ShouldNotBeAuthorized(this CommandResult result)
    {
        if (result.IsAuthorized)
        {
            throw new CommandResultAssertionException("Expected command to not be authorized, but it was.");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> has no exceptions.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result has exceptions.</exception>
    public static void ShouldNotHaveExceptions(this CommandResult result)
    {
        if (result.HasExceptions)
        {
            var messages = string.Join(", ", result.ExceptionMessages);
            throw new CommandResultAssertionException($"Expected command to have no exceptions, but it had: {messages}");
        }
    }

    /// <summary>
    /// Asserts that the <see cref="CommandResult"/> has exceptions.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to assert.</param>
    /// <exception cref="CommandResultAssertionException">Thrown when the result has no exceptions.</exception>
    public static void ShouldHaveExceptions(this CommandResult result)
    {
        if (!result.HasExceptions)
        {
            throw new CommandResultAssertionException("Expected command to have exceptions, but it had none.");
        }
    }

    static string BuildFailureReasons(CommandResult result)
    {
        var reasons = new List<string>();
        if (!result.IsAuthorized)
        {
            reasons.Add($"Not authorized: {result.AuthorizationFailureReason}");
        }

        if (!result.IsValid)
        {
            var errors = string.Join(", ", result.ValidationResults.Select(v => v.Message));
            reasons.Add($"Validation errors: {errors}");
        }

        if (result.HasExceptions)
        {
            var messages = string.Join(", ", result.ExceptionMessages);
            reasons.Add($"Exceptions: {messages}");
        }

        return reasons.Count > 0
            ? $" Reasons: {string.Join("; ", reasons)}"
            : string.Empty;
    }
}
