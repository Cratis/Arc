// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Converts between the expressions of a manifest file and the expressions the engine evaluates.
/// </summary>
public static class ManifestExpressions
{
    static readonly Dictionary<string, ComparisonOperator> _operators = new()
    {
        ["isTrue"] = ComparisonOperator.IsTrue,
        ["equal"] = ComparisonOperator.Equal,
        ["notEqual"] = ComparisonOperator.NotEqual,
        ["in"] = ComparisonOperator.In,
        ["notIn"] = ComparisonOperator.NotIn,
        ["greaterThan"] = ComparisonOperator.GreaterThan,
        ["greaterThanOrEqual"] = ComparisonOperator.GreaterThanOrEqual,
        ["lessThan"] = ComparisonOperator.LessThan,
        ["lessThanOrEqual"] = ComparisonOperator.LessThanOrEqual
    };

    /// <summary>
    /// Converts a manifest expression to the expression the engine evaluates.
    /// </summary>
    /// <param name="expression">The manifest expression.</param>
    /// <returns>The engine expression.</returns>
    /// <exception cref="InvalidDataException">The expression holds a condition that is code, or an operator that is not known.</exception>
    public static AvailabilityExpression ToExpression(this ManifestExpression expression) => expression switch
    {
        ManifestFor f => new AvailableForAudience(new AudienceName(f.Audience)),
        ManifestNot n => new AvailableNotAudience(new AudienceName(n.Audience)),
        ManifestWhen w => new AvailableWhen(w.Condition.ToCondition()),
        ManifestWith w => new AvailableWith(w.Block),
        ManifestAll a => new AvailabilityAnd([.. a.Items.Select(ToExpression)]),
        ManifestAny a => new AvailabilityOr([.. a.Items.Select(ToExpression)]),
        _ => throw new InvalidDataException($"Unknown expression {expression.GetType().Name}.")
    };

    /// <summary>
    /// Converts a manifest condition to the condition the engine evaluates.
    /// </summary>
    /// <param name="condition">The manifest condition.</param>
    /// <returns>The engine condition.</returns>
    /// <exception cref="InvalidDataException">The condition is code, or its operator is not known.</exception>
    public static IAvailabilityCondition ToCondition(this ManifestCondition condition) => condition switch
    {
        ManifestCompare c when _operators.TryGetValue(c.Operator, out var op) => new ContextComparison(c.Path, op, [.. c.Operands.Select(ToOperand)]),
        ManifestCompare c => throw new InvalidDataException($"Unknown operator '{c.Operator}'."),
        ManifestAllConditions a => Combine(true, a.Items),
        ManifestAnyConditions a => Combine(false, a.Items),
        ManifestNamedCondition n => throw new InvalidDataException($"The condition '{n.Name}' is code and cannot be evaluated from a manifest file."),
        _ => throw new InvalidDataException($"Unknown condition {condition.GetType().Name}.")
    };

    /// <summary>
    /// Converts an engine expression to a manifest expression.
    /// </summary>
    /// <param name="expression">The engine expression.</param>
    /// <returns>The manifest expression.</returns>
    /// <exception cref="InvalidOperationException">The expression is not one the engine knows.</exception>
    public static ManifestExpression ToManifest(this AvailabilityExpression expression) => expression switch
    {
        AvailableForAudience f => new ManifestFor(f.Audience.Value),
        AvailableNotAudience n => new ManifestNot(n.Audience.Value),
        AvailableWhen w => new ManifestWhen(ToManifest(w.Condition)),
        AvailableWith w => new ManifestWith(w.Name),
        AvailabilityAnd a => new ManifestAll([.. a.Parts.Select(ToManifest)]),
        AvailabilityOr o => new ManifestAny([.. o.Parts.Select(ToManifest)]),
        _ => throw new InvalidOperationException($"Unknown expression {expression.GetType().Name}.")
    };

    /// <summary>
    /// Converts an engine condition to a manifest condition. A condition that is code becomes a named condition.
    /// </summary>
    /// <param name="condition">The engine condition.</param>
    /// <returns>The manifest condition.</returns>
    public static ManifestCondition ToManifest(this IAvailabilityCondition condition) => condition switch
    {
        ContextComparison c => new ManifestCompare(c.Path, OperatorName(c.Operator), [.. c.Operands.Select(ToElement)]),
        CompositeCondition c => c.All
            ? new ManifestAllConditions([ToManifest(c.Left), ToManifest(c.Right)])
            : new ManifestAnyConditions([ToManifest(c.Left), ToManifest(c.Right)]),
        _ => new ManifestNamedCondition(condition.GetType().FullName ?? condition.GetType().Name, condition.Description)
    };

    static IAvailabilityCondition Combine(bool all, IReadOnlyList<ManifestCondition> items)
    {
        var conditions = items.Select(ToCondition).ToList();
        if (conditions.Count == 0)
        {
            throw new InvalidDataException("A combined condition needs at least one condition.");
        }

        return conditions.Skip(1).Aggregate(conditions[0], (left, right) => new CompositeCondition(all, left, right));
    }

    static string OperatorName(ComparisonOperator op) => _operators.First(_ => _.Value == op).Key;

    static object ToOperand(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw new InvalidDataException($"An operand must be a string, a number or a boolean, not {element.ValueKind}.")
    };

    static JsonElement ToElement(object operand) => JsonSerializer.SerializeToElement(operand);
}
