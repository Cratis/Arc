// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// A comparison of a provided context value, which compiles to a Screenplay <c>when</c>.
/// </summary>
/// <param name="ReadModel">The type of the provided read model the value is read from.</param>
/// <param name="Path">The path from the read model to the value, in camelCase.</param>
/// <param name="Operator">The operator.</param>
/// <param name="Operands">The operands: none for <see cref="ComparisonOperator.IsTrue"/>, one for a single comparison, any number for a set.</param>
public sealed record ComparisonTerm(Type ReadModel, string Path, ComparisonOperator Operator, IReadOnlyList<object> Operands) : ConditionTerm
{
    /// <summary>
    /// Gets the name the read model is provided under, which is the name of its type in camelCase.
    /// </summary>
    public string ContextName => ContextNames.Of(ReadModel);

    /// <inheritdoc/>
    public override IAvailabilityCondition ToCondition(Func<Type, IAvailabilityCondition> conditions) =>
        new ContextComparison($"{ContextName}.{Path}", Operator, [.. Operands]);
}
