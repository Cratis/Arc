// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRoot"/>.
/// </summary>
public class AggregateRoot : IAggregateRoot
{
    /// <summary>
    /// Context of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal IAggregateRootContext _context = default!;

    /// <summary>
    /// Mutation of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal IAggregateRootMutation _mutation = default!;

    readonly List<ValidationResult> _validationResults = [];

    /// <summary>
    /// Gets a value indicating whether the aggregate root is new.
    /// </summary>
    protected bool IsNew => !_context.HasEvents;

    /// <inheritdoc/>
    public async Task Apply(object @event) => await _mutation.Apply(@event);

    /// <inheritdoc/>
    public async Task<AggregateRootCommitResult> Commit()
    {
        if (_validationResults.Exists(v => v.Severity == ValidationResultSeverity.Error))
        {
            return AggregateRootCommitResult.WithErrors(_validationResults);
        }

        var result = await _mutation.Commit();
        await _mutation.Mutator.Dehydrate();

        if (_validationResults.Count == 0)
        {
            return result;
        }

        return new AggregateRootCommitResult
        {
            Events = result.Events,
            SequenceNumbers = result.SequenceNumbers,
            ConstraintViolations = result.ConstraintViolations,
            ConcurrencyViolations = result.ConcurrencyViolations,
            Errors = result.Errors,
            ValidationResults = _validationResults.ToArray()
        };
    }

    /// <summary>
    /// Chronicle Internal: Invoke the OnActivate method.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task InternalOnActivate() => OnActivate();

    /// <summary>
    /// Reports a failed validation with a message and a specific severity.
    /// The failure is accumulated and becomes part of the <see cref="AggregateRootCommitResult"/>
    /// when <see cref="Commit"/> is called. Failures with <see cref="ValidationResultSeverity.Error"/>
    /// severity will prevent the commit from occurring.
    /// </summary>
    /// <param name="message">The human-readable error message describing the failure.</param>
    /// <param name="severity">
    /// The <see cref="ValidationResultSeverity"/> of the failure.
    /// Defaults to <see cref="ValidationResultSeverity.Error"/>, which blocks the commit.
    /// </param>
    protected void Failed(string message, ValidationResultSeverity severity = ValidationResultSeverity.Error) =>
        _validationResults.Add(new ValidationResult(severity, message, [], new object()));

    /// <summary>
    /// Called when the aggregate root is ready to be activated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivate() => Task.CompletedTask;
}
