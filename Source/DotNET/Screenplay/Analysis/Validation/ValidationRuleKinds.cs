// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Maps the rule builders of a validator onto the declarative rules Screenplay has.
/// </summary>
/// <remarks>
/// Only rules whose meaning survives without the code behind them are mapped. The collection forms exist because a
/// rule declared for each element of a collection means something different from the same rule on the collection.
/// </remarks>
public static class ValidationRuleKinds
{
    /// <summary>
    /// The pattern name the grammar knows for an email address.
    /// </summary>
    public const string EmailPattern = "email";

    static readonly Dictionary<string, ValidationRuleKind> _single = new(StringComparer.Ordinal)
    {
        { "NotEmpty", ValidationRuleKind.NotEmpty },
        { "NotNull", ValidationRuleKind.NotEmpty },
        { "MaximumLength", ValidationRuleKind.Max },
        { "MinimumLength", ValidationRuleKind.Min },
        { "Length", ValidationRuleKind.Length },
        { "GreaterThan", ValidationRuleKind.GreaterThan },
        { "GreaterThanOrEqualTo", ValidationRuleKind.GreaterThanOrEqual },
        { "LessThan", ValidationRuleKind.LessThan },
        { "LessThanOrEqualTo", ValidationRuleKind.LessThanOrEqual },
        { "Equal", ValidationRuleKind.Equal },
        { "Matches", ValidationRuleKind.Matches },
        { "EmailAddress", ValidationRuleKind.Matches }
    };

    static readonly Dictionary<string, ValidationRuleKind> _each = new(StringComparer.Ordinal)
    {
        { "GreaterThan", ValidationRuleKind.AllGreaterThan },
        { "GreaterThanOrEqualTo", ValidationRuleKind.AllGreaterThanOrEqual }
    };

    /// <summary>
    /// Tries to map a rule builder onto a declarative rule.
    /// </summary>
    /// <param name="method">The name of the rule builder.</param>
    /// <param name="forEach">Whether the rule was declared for each element of a collection.</param>
    /// <param name="kind">The declarative rule.</param>
    /// <returns>True when the rule builder has a counterpart.</returns>
    public static bool TryResolve(string method, bool forEach, out ValidationRuleKind kind) =>
        forEach ? _each.TryGetValue(method, out kind) : _single.TryGetValue(method, out kind);

    /// <summary>
    /// Determines whether a rule builder carries meaning that only the code behind it has.
    /// </summary>
    /// <param name="method">The name of the rule builder.</param>
    /// <returns>True when the rule builder is a rule rather than a modifier.</returns>
    public static bool IsRule(string method) => _single.ContainsKey(method) || _each.ContainsKey(method);
}
