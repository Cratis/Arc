// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Arc.Testing.Validation;

/// <summary>
/// Provides assertions for validators run directly in a command validation context.
/// </summary>
/// <remarks>
/// Works with any model, including concepts. No pipeline, discovery or constructor activation is exercised.
/// All failure severities reject, and validator exceptions propagate without ValidatorInvoker substitution.
/// </remarks>
public static class ValidatorShouldExtensions
{
    /// <summary>
    /// Asserts that the validator accepts the command without any failures.
    /// </summary>
    /// <typeparam name="TCommand">The command or other model type.</typeparam>
    /// <param name="validator">The validator to run.</param>
    /// <param name="command">The value to validate.</param>
    /// <exception cref="ArgumentNullException">The command is null.</exception>
    /// <exception cref="ValidatorAssertionException">Validation failed or requires the asynchronous helper.</exception>
    public static void ShouldAccept<TCommand>(this IValidator<TCommand> validator, TCommand command) =>
        AssertAccepted(Validate(validator, command, nameof(ShouldAcceptAsync)).Errors);

    /// <summary>
    /// Asserts asynchronously that the validator accepts the command without any failures.
    /// </summary>
    /// <typeparam name="TCommand">The command or other model type.</typeparam>
    /// <param name="validator">The validator to run.</param>
    /// <param name="command">The value to validate.</param>
    /// <param name="cancellationToken">The cancellation token passed to the validator.</param>
    /// <returns>A task representing the validation and assertion.</returns>
    /// <exception cref="ArgumentNullException">The command is null.</exception>
    /// <exception cref="ValidatorAssertionException">Validation failed.</exception>
    public static async Task ShouldAcceptAsync<TCommand>(this IValidator<TCommand> validator, TCommand command, CancellationToken cancellationToken = default) =>
        AssertAccepted((await validator.ValidateAsync(ContextFor(command), cancellationToken)).Errors);

    /// <summary>
    /// Asserts that the validator rejects the command with the exact message, using ordinal comparison.
    /// </summary>
    /// <typeparam name="TCommand">The command or other model type.</typeparam>
    /// <param name="validator">The validator to run.</param>
    /// <param name="command">The value to validate.</param>
    /// <param name="because">The exact expected error message.</param>
    /// <exception cref="ArgumentNullException">The command is null.</exception>
    /// <exception cref="ValidatorAssertionException">No matching failure exists or validation requires the asynchronous helper.</exception>
    public static void ShouldReject<TCommand>(this IValidator<TCommand> validator, TCommand command, string because) =>
        AssertRejected(Validate(validator, command, nameof(ShouldRejectAsync)).Errors, because);

    /// <summary>
    /// Asserts asynchronously that the validator rejects the command with the exact message, using ordinal comparison.
    /// </summary>
    /// <typeparam name="TCommand">The command or other model type.</typeparam>
    /// <param name="validator">The validator to run.</param>
    /// <param name="command">The value to validate.</param>
    /// <param name="because">The exact expected error message.</param>
    /// <param name="cancellationToken">The cancellation token passed to the validator.</param>
    /// <returns>A task representing the validation and assertion.</returns>
    /// <exception cref="ArgumentNullException">The command is null.</exception>
    /// <exception cref="ValidatorAssertionException">No matching failure exists.</exception>
    public static async Task ShouldRejectAsync<TCommand>(this IValidator<TCommand> validator, TCommand command, string because, CancellationToken cancellationToken = default) =>
        AssertRejected((await validator.ValidateAsync(ContextFor(command), cancellationToken)).Errors, because);

    static ValidationContext<TCommand> ContextFor<TCommand>(TCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var context = new ValidationContext<TCommand>(command);
        context.SetCommand();
        return context;
    }

    static FluentValidation.Results.ValidationResult Validate<TCommand>(IValidator<TCommand> validator, TCommand command, string asynchronousHelper)
    {
        var context = ContextFor(command);
        try
        {
            return validator.Validate(context);
        }
        catch (AsyncValidatorInvokedSynchronouslyException)
        {
            throw new ValidatorAssertionException($"The validator has asynchronous rules; use {asynchronousHelper}.");
        }
    }

    static void AssertAccepted(List<ValidationFailure> failures)
    {
        if (failures.Count > 0)
        {
            throw new ValidatorAssertionException($"Expected validator to accept the command, but it rejected it. Actual: {Describe(failures)}.");
        }
    }

    static void AssertRejected(List<ValidationFailure> failures, string because)
    {
        if (failures.Count == 0)
        {
            throw new ValidatorAssertionException($"Expected validator to reject the command with '{because}', but it accepted it.");
        }

        if (!failures.Exists(failure => string.Equals(failure.ErrorMessage, because, StringComparison.Ordinal)))
        {
            throw new ValidatorAssertionException($"Expected validator to reject the command, but no failure said '{because}'. Actual: {Describe(failures)}.");
        }
    }

    static string Describe(IEnumerable<ValidationFailure> failures) =>
        string.Join(", ", failures.Select(failure => $"{failure.PropertyName}: '{failure.ErrorMessage}' ({failure.Severity})"));
}
