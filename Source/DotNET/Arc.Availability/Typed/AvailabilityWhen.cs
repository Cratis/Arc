// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.Availability;

/// <summary>
/// Starts a 'when' condition in an <see cref="AvailabilityBlock"/>.
/// </summary>
/// <remarks>
/// <see cref="On{TReadModel}"/> is the restricted form: a comparison of a provided read model that compiles to the
/// Screenplay <c>when</c> grammar. <see cref="Condition{TCondition}"/> is for anything else and is emitted as an opaque condition.
/// </remarks>
public sealed class AvailabilityWhen
{
    /// <summary>
    /// Starts a comparison of a value of a provided read model.
    /// </summary>
    /// <typeparam name="TReadModel">The read model, provided with <see cref="ProvideContextAttribute{TReadModel}"/>.</typeparam>
    /// <param name="value">A property, or a chain of properties, of the read model.</param>
    /// <returns>The operand to compare.</returns>
    public ContextOperand On<TReadModel>(Expression<Func<TReadModel, object?>> value) =>
        new(typeof(TReadModel), PathOf(value.Body));

    /// <summary>
    /// States the condition as a class, for anything the restricted form cannot say.
    /// </summary>
    /// <typeparam name="TCondition">The condition, which can inject services.</typeparam>
    /// <returns>The condition.</returns>
    public ConditionTerm Condition<TCondition>()
        where TCondition : IAvailabilityCondition => new ConditionClassTerm(typeof(TCondition));

    static string PathOf(Expression expression)
    {
        var segments = new List<string>();
        var current = expression;
        while (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            current = unary.Operand;
        }

        while (current is MemberExpression member)
        {
            segments.Insert(0, ContextNames.CamelCase(member.Member.Name));
            current = member.Expression;
        }

        if (current is not ParameterExpression || segments.Count == 0)
        {
            throw new ArgumentException("A condition reads a property, or a chain of properties, of the read model; anything else is not a Screenplay 'when'. Use a condition class instead.");
        }

        return string.Join('.', segments);
    }
}
