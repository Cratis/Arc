// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Helpers for deciding which validation results on a <see cref="CommandResult"/> block command execution
/// relative to an allowed severity level.
/// </summary>
static class CommandValidationResults
{
    static readonly ConcurrentDictionary<Type, (bool HasPolicy, ValidationResultSeverity Severity)> _policies = new();

    /// <summary>
    /// Combines a caller threshold with the command's declared blocking floor. Only attributed commands
    /// reject Unknown; unannotated commands keep their existing filtering behavior.
    /// </summary>
    /// <param name="commandType">The type declaring the command policy.</param>
    /// <param name="callerSeverity">The caller's allowed severity.</param>
    /// <returns>The effective threshold and whether Unknown blocks.</returns>
    /// <exception cref="InvalidCommandValidationSeverity">Thrown when the declared severity is unsupported.</exception>
    public static (ValidationResultSeverity? AllowedSeverity, bool BlockUnknown) ForCommand(Type commandType, ValidationResultSeverity? callerSeverity)
    {
        var policy = _policies.GetOrAdd(commandType, static type =>
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (Attribute.GetCustomAttribute(current, typeof(BlockOnValidationSeverityAttribute), inherit: false) is BlockOnValidationSeverityAttribute attribute)
                {
                    return (true, attribute.Severity);
                }
            }

            return (false, ValidationResultSeverity.Unknown);
        });
        if (!policy.HasPolicy)
        {
            return (callerSeverity, false);
        }

        if (policy.Severity is < ValidationResultSeverity.Unknown or > ValidationResultSeverity.Error)
        {
            throw new InvalidCommandValidationSeverity(commandType);
        }

        // The existing filter keeps values strictly above the allowed threshold. The policy is inclusive.
        var maximumAllowed = (ValidationResultSeverity)((int)policy.Severity - 1);
        return (callerSeverity is null || callerSeverity > maximumAllowed ? maximumAllowed : callerSeverity, true);
    }

    /// <summary>
    /// Gets the validation results that block execution for the given allowed severity.
    /// </summary>
    /// <param name="results">The validation results to filter.</param>
    /// <param name="allowedSeverity">The maximum allowed severity; results with higher severity block.</param>
    /// <param name="blockUnknown">Whether Unknown results also block.</param>
    /// <returns>The blocking validation results.</returns>
    /// <remarks>
    /// When <paramref name="allowedSeverity"/> is <see langword="null"/>, only errors block execution.
    /// Otherwise, results with a severity higher than <paramref name="allowedSeverity"/> block execution.
    /// </remarks>
    public static IEnumerable<ValidationResult> Blocking(IEnumerable<ValidationResult> results, ValidationResultSeverity? allowedSeverity, bool blockUnknown = false) =>
        allowedSeverity is null
            ? results.Where(_ => _.Severity == ValidationResultSeverity.Error || (blockUnknown && _.Severity == ValidationResultSeverity.Unknown))
            : results.Where(_ => _.Severity > allowedSeverity || (blockUnknown && _.Severity == ValidationResultSeverity.Unknown));

    /// <summary>
    /// Determines whether the given <see cref="CommandResult"/> blocks execution for the allowed severity.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to evaluate.</param>
    /// <param name="allowedSeverity">The maximum allowed validation severity.</param>
    /// <param name="blockUnknown">Whether Unknown results also block.</param>
    /// <returns>True if the result should stop command execution; otherwise, false.</returns>
    public static bool IsBlocking(CommandResult result, ValidationResultSeverity? allowedSeverity, bool blockUnknown = false) =>
        !result.IsAuthorized ||
        result.HasExceptions ||
        Blocking(result.ValidationResults, allowedSeverity, blockUnknown).Any();
}
