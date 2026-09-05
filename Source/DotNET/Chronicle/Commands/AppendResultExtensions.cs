// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Extension methods for converting append results to command results.
/// </summary>
public static class AppendResultExtensions
{
    /// <summary>
    /// Convert an <see cref="IAppendResult"/> to a <see cref="CommandResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="IAppendResult"/> to convert.</param>
    /// <returns>A <see cref="CommandResult"/>.</returns>
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
}
