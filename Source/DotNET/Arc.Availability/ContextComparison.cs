// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// A condition that compares a context value with literals. An absent value is unknown, whatever the operator.
/// </summary>
/// <param name="path">The context path to read, for example 'vessel.commercialPackage'.</param>
/// <param name="op">The operator.</param>
/// <param name="operands">The literals to compare with. None for <see cref="ComparisonOperator.IsTrue"/>.</param>
public class ContextComparison(string path, ComparisonOperator op, params object[] operands) : IAvailabilityCondition
{
    /// <summary>
    /// Gets the context path that is read.
    /// </summary>
    public string Path => path;

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public ComparisonOperator Operator => op;

    /// <summary>
    /// Gets the literals that are compared with.
    /// </summary>
    public IReadOnlyList<object> Operands => operands;

    /// <inheritdoc/>
    public string Description => op == ComparisonOperator.IsTrue ? path : $"{path} {op} {string.Join(", ", operands)}";

    /// <inheritdoc/>
    public async ValueTask<AvailabilityOutcome> Evaluate(IContextValueResolver context, CancellationToken cancellationToken)
    {
        var read = await context.Resolve(path, cancellationToken);
        if (!read.IsPresent)
        {
            return AvailabilityOutcome.Unknown;
        }

        var value = read.Value!;
        var holds = op switch
        {
            ComparisonOperator.IsTrue => value is true,
            ComparisonOperator.Equal => AreEqual(value, operands[0]),
            ComparisonOperator.NotEqual => !AreEqual(value, operands[0]),
            ComparisonOperator.In => operands.Any(_ => AreEqual(value, _)),
            ComparisonOperator.NotIn => !operands.Any(_ => AreEqual(value, _)),
            ComparisonOperator.GreaterThan => Compare(value, operands[0]) > 0,
            ComparisonOperator.GreaterThanOrEqual => Compare(value, operands[0]) >= 0,
            ComparisonOperator.LessThan => Compare(value, operands[0]) < 0,
            ComparisonOperator.LessThanOrEqual => Compare(value, operands[0]) <= 0,
            _ => throw new NotSupportedException($"Unsupported comparison operator {op}.")
        };

        return holds ? AvailabilityOutcome.Available : AvailabilityOutcome.Unavailable;
    }

    static bool AreEqual(object value, object operand) =>
        IsNumber(value) && IsNumber(operand)
            ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) == Convert.ToDecimal(operand, CultureInfo.InvariantCulture)
            : value.Equals(operand);

    static int Compare(object value, object operand)
    {
        if (IsNumber(value) && IsNumber(operand))
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(operand, CultureInfo.InvariantCulture));
        }

        if (value is IComparable comparable && value.GetType() == operand.GetType())
        {
            return comparable.CompareTo(operand);
        }

        throw new InvalidOperationException($"Cannot order a {value.GetType().Name} against a {operand.GetType().Name}.");
    }

    static bool IsNumber(object value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}
