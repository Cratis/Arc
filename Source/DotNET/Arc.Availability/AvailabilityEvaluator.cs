// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Evaluates gate expressions three-valued, in authored order, stopping at the first decisive part.
/// </summary>
/// <remarks>
/// A part after a decisive one is not evaluated, so a failure in it does not surface.
/// </remarks>
/// <param name="catalog">The availability blocks of the model.</param>
[IgnoreConvention]
public class AvailabilityEvaluator(AvailabilityCatalog catalog) : IAvailabilityEvaluator
{
    /// <inheritdoc/>
    public async ValueTask<AvailabilityResult> Evaluate(AvailabilityExpression gate, ContextValueResolver context, CancellationToken cancellationToken = default)
    {
        var (outcome, causes) = await Evaluate(gate, context, [], cancellationToken);
        return outcome == AvailabilityOutcome.Available ? AvailabilityResult.Available : new AvailabilityResult(outcome, causes);
    }

    /// <inheritdoc/>
    public ValueTask<AvailabilityResult> EvaluatePath(IEnumerable<AvailabilityExpression> gates, ContextValueResolver context, CancellationToken cancellationToken = default) =>
        Evaluate(new AvailabilityAnd([.. gates]), context, cancellationToken);

    async Task<(AvailabilityOutcome Outcome, List<string> Causes)> Evaluate(
        AvailabilityExpression expression,
        ContextValueResolver context,
        List<string> blocks,
        CancellationToken cancellationToken)
    {
        switch (expression)
        {
            case AvailableForAudience audience:
                var audiences = await context.GetAudiences(cancellationToken);
                var cause = $"for {audience.Audience.Value}";
                if (audiences is null)
                {
                    return (AvailabilityOutcome.Unknown, [cause]);
                }

                return audiences.Holds(audience.Audience) ? (AvailabilityOutcome.Available, []) : (AvailabilityOutcome.Unavailable, [cause]);

            case AvailableNotAudience notAudience:
                var held = await context.GetAudiences(cancellationToken);
                var notCause = $"not {notAudience.Audience.Value}";
                if (held is null)
                {
                    return (AvailabilityOutcome.Unknown, [notCause]);
                }

                return held.Holds(notAudience.Audience) ? (AvailabilityOutcome.Unavailable, [notCause]) : (AvailabilityOutcome.Available, []);

            case AvailableWhen when_:
                var outcome = await when_.Condition.Evaluate(context, cancellationToken);
                return (outcome, outcome == AvailabilityOutcome.Available ? [] : [$"when {when_.Condition.Description}"]);

            case AvailableWith with:
                if (blocks.Contains(with.Name))
                {
                    throw new CircularAvailabilityException([.. blocks, with.Name]);
                }

                blocks.Add(with.Name);
                try
                {
                    return await Evaluate(catalog.Get(with.Name).Expression, context, blocks, cancellationToken);
                }
                finally
                {
                    blocks.RemoveAt(blocks.Count - 1);
                }

            case AvailabilityAnd and:
                return await All(and.Parts, context, blocks, cancellationToken);

            case AvailabilityOr or:
                return await Any(or.Parts, context, blocks, cancellationToken);

            default:
                throw new NotSupportedException($"Unsupported availability expression {expression.GetType().Name}.");
        }
    }

    async Task<(AvailabilityOutcome Outcome, List<string> Causes)> All(
        IReadOnlyList<AvailabilityExpression> parts,
        ContextValueResolver context,
        List<string> blocks,
        CancellationToken cancellationToken)
    {
        var unknown = new List<string>();
        foreach (var part in parts)
        {
            var (outcome, causes) = await Evaluate(part, context, blocks, cancellationToken);
            switch (outcome)
            {
                case AvailabilityOutcome.Unavailable:
                    return (outcome, causes);
                case AvailabilityOutcome.Unknown:
                    unknown.AddRange(causes);
                    break;
            }
        }

        return unknown.Count > 0 ? (AvailabilityOutcome.Unknown, unknown) : (AvailabilityOutcome.Available, []);
    }

    async Task<(AvailabilityOutcome Outcome, List<string> Causes)> Any(
        IReadOnlyList<AvailabilityExpression> parts,
        ContextValueResolver context,
        List<string> blocks,
        CancellationToken cancellationToken)
    {
        var unavailable = new List<string>();
        var unknown = false;
        foreach (var part in parts)
        {
            var (outcome, causes) = await Evaluate(part, context, blocks, cancellationToken);
            if (outcome == AvailabilityOutcome.Available)
            {
                return (outcome, []);
            }

            unknown |= outcome == AvailabilityOutcome.Unknown;
            unavailable.AddRange(causes);
        }

        // An empty "or" holds nothing: unavailable.
        return (unknown ? AvailabilityOutcome.Unknown : AvailabilityOutcome.Unavailable, unavailable);
    }
}
