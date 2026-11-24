// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// A gate expression: 'for', 'when' and 'with' parts combined with 'and', 'or' and parentheses.
/// </summary>
/// <remarks>
/// The tree keeps authored order, and parentheses are the tree's shape.
/// </remarks>
public abstract record AvailabilityExpression
{
    /// <summary>
    /// Creates 'for' over one or more audiences, which holds when any is held.
    /// </summary>
    /// <param name="audiences">The audiences.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression For(params AudienceName[] audiences) =>
        audiences.Length == 1 ? new AvailableForAudience(audiences[0]) : new AvailabilityOr([.. audiences.Select(_ => new AvailableForAudience(_))]);

    /// <summary>
    /// Creates 'when' over a condition.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression When(IAvailabilityCondition condition) => new AvailableWhen(condition);

    /// <summary>
    /// Creates 'with' over a named availability block.
    /// </summary>
    /// <param name="name">The name of the block.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression With(string name) => new AvailableWith(name);

    /// <summary>
    /// Combines expressions with 'and', in the order given.
    /// </summary>
    /// <param name="parts">The parts.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression AllOf(params AvailabilityExpression[] parts) => new AvailabilityAnd(parts);

    /// <summary>
    /// Combines expressions with 'or', in the order given.
    /// </summary>
    /// <param name="parts">The parts.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression AnyOf(params AvailabilityExpression[] parts) => new AvailabilityOr(parts);

    /// <summary>
    /// Expands the expression into disjunctive normal form: a list of alternatives, any one of which satisfies it.
    /// </summary>
    /// <returns>The alternatives, in authored order, without repeats.</returns>
    /// <exception cref="InvalidOperationException">The expression uses 'with'; pass the catalog that defines the blocks.</exception>
    public IReadOnlyList<AvailabilityAlternative> Alternatives() => Alternatives(null);

    /// <summary>
    /// Expands the expression into disjunctive normal form, replacing each 'with' by the block it names.
    /// </summary>
    /// <param name="catalog">The catalog that defines the blocks used by 'with'.</param>
    /// <returns>The alternatives, in authored order, without repeats.</returns>
    /// <exception cref="UndefinedAvailabilityException">A block that is used is not defined.</exception>
    /// <exception cref="CircularAvailabilityException">Blocks build on each other in a cycle.</exception>
    public IReadOnlyList<AvailabilityAlternative> Alternatives(AvailabilityCatalog? catalog) => [.. Expand(this, catalog, []).Distinct()];

    static List<AvailabilityAlternative> Expand(AvailabilityExpression expression, AvailabilityCatalog? catalog, List<string> chain)
    {
        switch (expression)
        {
            case AvailableForAudience audience:
                return [new([audience.Audience], [])];
            case AvailableWhen whenExpression:
                return [new([], [whenExpression.Condition.Description])];
            case AvailableWith with:
                if (catalog is null)
                {
                    throw new InvalidOperationException($"The expression uses 'with {with.Name}'; pass the catalog that defines it.");
                }

                if (chain.Contains(with.Name))
                {
                    throw new CircularAvailabilityException([.. chain, with.Name]);
                }

                chain.Add(with.Name);
                var expanded = Expand(catalog.Get(with.Name).Expression, catalog, chain);
                chain.RemoveAt(chain.Count - 1);
                return expanded;
            case AvailabilityOr orExpression:
                return [.. orExpression.Parts.SelectMany(_ => Expand(_, catalog, chain))];
            case AvailabilityAnd andExpression:
                return andExpression.Parts.Aggregate(
                    new List<AvailabilityAlternative> { AvailabilityAlternative.Unconditional },
                    (soFar, part) =>
                    {
                        var next = Expand(part, catalog, chain);
                        return [.. soFar.SelectMany(left => next.Select(left.And)).Distinct()];
                    });
            default:
                throw new NotSupportedException($"Unsupported availability expression {expression.GetType().Name}.");
        }
    }
}
