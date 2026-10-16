// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Recovers literal-only date rules without extending concept or client validation contracts.
/// </summary>
internal static class DateValidationRules
{
    static readonly HashSet<string> _comparisons = new(StringComparer.Ordinal) { "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo", "Equal", "NotEqual" };

    /// <summary>
    /// Gets the selected property's type.
    /// </summary>
    /// <param name="root">The RuleFor call.</param>
    /// <param name="model">Its semantic model.</param>
    /// <returns>The property type or null.</returns>
    internal static ITypeSymbol? Subject(InvocationExpressionSyntax root, SemanticModel model) =>
        InvocationChain.ArgumentOf(root) is LambdaExpressionSyntax { ExpressionBody: { } body } ? model.GetTypeInfo(body).Type : null;

    /// <summary>
    /// Determines whether a rule compares a date subject.
    /// </summary>
    /// <param name="name">The rule method.</param>
    /// <param name="subject">The subject type.</param>
    /// <param name="condition">The condition guarding the rule, if any.</param>
    /// <returns>Whether date admission applies.</returns>
    internal static bool Applies(string name, ITypeSymbol? subject, string? condition) =>
        condition is null && DateComparisonOperands.Primitive(subject) is not null && _comparisons.Contains(name);

    /// <summary>
    /// Reads a literal date rule or reports why it remains in code.
    /// </summary>
    /// <param name="call">The rule call.</param>
    /// <param name="target">The validated property.</param>
    /// <param name="site">Where the rule is read.</param>
    /// <returns>The rule or null.</returns>
    internal static ValidationRuleModel? Read(InvocationExpressionSyntax call, DateRuleTarget target, DateRuleSite site)
    {
        var argument = InvocationChain.ArgumentOf(call);
        var literal = argument is null ? null : DateComparisonOperands.Literal(argument, site.Model);
        var reason = Refusal(call, argument, target, site.Model) ?? (literal is null ? "a canonical calendar date or an instant with an explicit offset or Z is required; clock operands such as Now, UtcNow and Today are not admitted" : null);
        if (reason is not null)
        {
            site.Diagnostics.Warning(ScreenplayDiagnosticCodes.UnmappableValidationRule, $"The date rule on '{site.Location}.{target.Property}' was left in code: {reason}", site.Location);

            return null;
        }
        if (DateComparisonOperands.Primitive(target.Subject) == ScreenplayPrimitive.DateTime)
        {
            DateComparisonOperands.ReportInputBinding(target.Property, site.Location, site.Diagnostics);
        }
        ValidationRuleKinds.TryResolve(InvocationChain.NameOf(call), false, out var kind);

        return new(target.Property, kind, literal, null);
    }

    static string? Refusal(InvocationExpressionSyntax call, ExpressionSyntax? argument, DateRuleTarget target, SemanticModel model)
    {
        var validator = model.GetEnclosingSymbol(call.SpanStart)?.ContainingType;
        var validated = validator is null ? null : ValidationReader.ValidatedTypeOf(validator);
        if (validated?.FindBase(WellKnownTypeNames.ConceptAs) is not null)
        {
            return "date rules on concepts are not admitted, because they also reach query inputs and client validators";
        }
        if (target.ForEach)
        {
            return "date comparisons on collection elements are not admitted";
        }

        return LambdaPaths.Read(argument) is not null ? "date validation rules require a literal, not another property" :
            argument is null || DateComparisonOperands.Primitive(model.GetTypeInfo(argument).Type) != DateComparisonOperands.Primitive(target.Subject) ? "Date and DateTime operands cannot be mixed" : null;
    }
}
