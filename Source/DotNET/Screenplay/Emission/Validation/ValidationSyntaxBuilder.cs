// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;
using SyntaxRuleKind = Cratis.Screenplay.Syntax.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.Emission.Validation;

/// <summary>
/// Builds the Screenplay <c>validate</c> block for a set of validation rules.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
public class ValidationSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Builds the validation blocks for a set of rules.
    /// </summary>
    /// <param name="rules">The rules to build for.</param>
    /// <param name="location">Where the rules were declared, for use in diagnostics.</param>
    /// <param name="impliedSubject">Whether the rules apply to an implied subject rather than a named property.</param>
    /// <returns>The validation blocks, empty when there is nothing to declare.</returns>
    /// <remarks>
    /// Rules inside a <c>concept</c> block carry no property name - the concept's own value is the implied subject -
    /// so every rule is emitted against <see cref="ValidationRuleSyntax.ConceptValue"/> regardless of the member the
    /// rule was declared on.
    /// </remarks>
    public IEnumerable<ValidateSyntax> Build(IEnumerable<ValidationRuleModel> rules, string location, bool impliedSubject = false)
    {
        var converted = new List<ValidationRuleSyntax>();

        foreach (var rule in rules)
        {
            var operand = ToOperand(rule);
            if (operand is null && rule.Kind != ModelRuleKind.NotEmpty)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.ValidationRuleWithoutOperand,
                    $"The '{rule.Kind}' rule on '{rule.Property}' carries no value to compare against and was left out",
                    location);
                continue;
            }

            converted.Add(new(
                impliedSubject ? ValidationRuleSyntax.ConceptValue : naming.ToPropertyPath(rule.Property),
                ToKind(rule.Kind),
                rule.Kind == ModelRuleKind.NotEmpty ? null : operand,
                ToMessage(rule.Message),
                SourceLocation.Start));
        }

        return converted.Count == 0 ? [] : [new DeclarativeValidateSyntax(converted, SourceLocation.Start)];
    }

    /// <summary>
    /// Converts the kind of a rule.
    /// </summary>
    /// <param name="kind">The kind to convert.</param>
    /// <returns>The Screenplay rule kind.</returns>
    static SyntaxRuleKind ToKind(ModelRuleKind kind) => kind switch
    {
        ModelRuleKind.Max => SyntaxRuleKind.Max,
        ModelRuleKind.Min => SyntaxRuleKind.Min,
        ModelRuleKind.GreaterThan => SyntaxRuleKind.GreaterThan,
        ModelRuleKind.GreaterThanOrEqual => SyntaxRuleKind.GreaterThanOrEqual,
        ModelRuleKind.LessThan => SyntaxRuleKind.LessThan,
        ModelRuleKind.LessThanOrEqual => SyntaxRuleKind.LessThanOrEqual,
        ModelRuleKind.Equal => SyntaxRuleKind.Equal,
        ModelRuleKind.Length => SyntaxRuleKind.Length,
        ModelRuleKind.Matches => SyntaxRuleKind.Matches,
        ModelRuleKind.AllGreaterThan => SyntaxRuleKind.AllGreaterThan,
        ModelRuleKind.AllGreaterThanOrEqual => SyntaxRuleKind.AllGreaterThanOrEqual,
        _ => SyntaxRuleKind.NotEmpty
    };

    /// <summary>
    /// Converts the operand of a rule.
    /// </summary>
    /// <param name="rule">The rule to convert the operand of.</param>
    /// <returns>The operand, or <see langword="null"/> when the rule carries none.</returns>
    /// <remarks>
    /// A pattern that is a bare word - <c>email</c> is the one the grammar knows - is written unquoted, because that
    /// is how the named patterns are referenced. Everything else is a string literal.
    /// <para>
    /// A rule comparing against another property of the same command carries the path it names rather than a value,
    /// and a rule operand is a host expression, so the path is written as one - <c>endDate &gt;= startDate</c> reads
    /// as what the developer wrote, where a literal would read as a comparison against the word.
    /// </para>
    /// </remarks>
    ExpressionSyntax? ToOperand(ValidationRuleModel rule)
    {
        if (rule.Value is null)
        {
            return null;
        }

        if (rule.Value is PropertyPathSource property)
        {
            return new PathExpressionSyntax(naming.ToPropertyPath(property.Path), SourceLocation.Start);
        }

        if (rule.Kind == ModelRuleKind.Matches && rule.Value is string pattern && ScreenplayIdentifier.IsBareIdentifier(pattern))
        {
            return new PathExpressionSyntax(pattern, SourceLocation.Start);
        }

        return LiteralConverter.Convert(rule.Value, naming);
    }

    /// <summary>
    /// Converts the message of a rule, leaving a localization key untouched.
    /// </summary>
    /// <param name="message">The message to convert.</param>
    /// <returns>The message, or <see langword="null"/> when there is none.</returns>
    string? ToMessage(string? message) =>
        ScreenplayIdentifier.IsLocalizationKey(message) ? message : naming.ToStringLiteral(message);
}
