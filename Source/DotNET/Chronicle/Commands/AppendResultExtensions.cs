// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Extension methods for converting append results to command results.
/// </summary>
public static class AppendResultExtensions
{
    /// <summary>
    /// Converts an append result using its constraint and concurrency outcomes.
    /// </summary>
    /// <param name="result">The append result.</param>
    /// <returns>The command result.</returns>
    public static CommandResult ToCommandResult(this IAppendResult result)
    {
        var commandResult = new CommandResult
        {
            CorrelationId = result.CorrelationId
        };

        if (result.HasErrors)
        {
            commandResult.ExceptionMessages = result.Errors.Select(e => e.Value);
        }

        if (result.HasConstraintViolations || result.HasConcurrencyViolations)
        {
            var validationResults = new List<ValidationResult>();

            if (result.HasConstraintViolations)
            {
                validationResults.AddRange(result.ConstraintViolations.Select(v => v.ToValidationResult()));
            }

            if (result.HasConcurrencyViolations)
            {
                if (result is AppendResult appendResult && appendResult.ConcurrencyViolation is not null)
                {
                    validationResults.Add(appendResult.ConcurrencyViolation.ToValidationResult());
                }
                else if (result is AppendManyResult appendManyResult)
                {
                    validationResults.AddRange(appendManyResult.ConcurrencyViolations.Select(_ => _.ToValidationResult()));
                }
            }

            commandResult.ValidationResults = validationResults;
        }

        return commandResult;
    }

    /// <summary>
    /// Converts an append outcome using the producing invocation's freshness provenance.
    /// </summary>
    /// <param name="result">The append result.</param>
    /// <param name="context">The producing invocation.</param>
    /// <param name="sequence">The event sequence that was appended to.</param>
    /// <returns>The command result.</returns>
    internal static CommandResult ToCommandResult(this IAppendResult result, CommandContext context, EventSequenceId sequence)
    {
        var commandResult = result.ToCommandResult();
        var freshness = FreshIdentifierScopes.For(context);
        commandResult.ValidationResults = commandResult.ValidationResults.Select(validation =>
            validation.State is ConcurrencyViolation violation && freshness.Owns(sequence, violation)
                ? GeneratedIdentifierCollision.CreateValidationResult(violation)
                : validation).ToArray();

        return commandResult;
    }
}
