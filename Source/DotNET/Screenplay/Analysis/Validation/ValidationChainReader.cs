// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Reads the rules one chain of a validator's constructor declares for one property.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <param name="paths">The paths of predicate implementations relative to the source root.</param>
/// <remarks>
/// A chain names a property once and then declares rule after rule on it, with messages attaching to whichever rule
/// they were written after. Counting what each call declared is what lets a message find the right rule.
/// </remarks>
public class ValidationChainReader(ScreenplayDiagnostics diagnostics, SourcePaths? paths)
{
    /// <summary>
    /// The call carrying the message shown when a rule is broken.
    /// </summary>
    public const string WithMessage = "WithMessage";

    /// <summary>
    /// The call constraining the length of a value, which in its two argument form is a range.
    /// </summary>
    public const string Length = "Length";

    /// <summary>
    /// The call holding the rules before it to a condition.
    /// </summary>
    public const string When = "When";

    /// <summary>
    /// The call holding the rules before it to a condition being false.
    /// </summary>
    public const string Unless = "Unless";

    static readonly HashSet<string> _modifiers = new(StringComparer.Ordinal)
    {
        "WithErrorCode", "WithSeverity", "WithName", "OverridePropertyName", "WithState",
        "Configure", "DependentRules", "Cascade", "OnFailure", "OnAnyFailure", "WhenAsync", "UnlessAsync"
    };
    static readonly HashSet<string> _conditions = new(StringComparer.Ordinal) { When, Unless, "WhenAsync", "UnlessAsync" };

    readonly ValidationOperands _operands = new(diagnostics);

    /// <summary>
    /// Initializes a chain reader without implementation file paths for named predicates.
    /// </summary>
    /// <param name="diagnostics">Where unmappable rules are reported.</param>
    public ValidationChainReader(ScreenplayDiagnostics diagnostics)
        : this(diagnostics, null)
    {
    }

    /// <summary>
    /// Reads one rule chain.
    /// </summary>
    /// <param name="chain">The chain to read.</param>
    /// <param name="forEach">Whether the rules were declared for each element of a collection.</param>
    /// <param name="semanticModel">The semantic model of the tree the chain lives in.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <param name="rules">The rules collected so far.</param>
    public void Read(
        InvocationChain chain,
        bool forEach,
        SemanticModel semanticModel,
        string location,
        IList<ValidationRuleModel> rules)
    {
        var property = LambdaPaths.Read(InvocationChain.ArgumentOf(chain.Root));
        if (property is null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"'{chain.Root}' does not name a property directly, so the rules declared on it were left out",
                location);

            return;
        }

        var preceding = 0;
        var scope = ValidationConditions.ScopeOf(chain, semanticModel);
        var conditional = scope is not null || chain.Calls.Any(call => _conditions.Contains(InvocationChain.NameOf(call)));

        foreach (var call in chain.Calls)
        {
            var condition = scope ?? ConditionFor(call, chain, semanticModel);
            var added = ReadCall(call, property, forEach, semanticModel, location, rules, preceding, conditional, condition);
            if (added != 0)
            {
                preceding = Math.Max(0, added);
            }
        }
    }

    static string? ConditionFor(InvocationExpressionSyntax rule, InvocationChain chain, SemanticModel semanticModel)
    {
        var following = chain.Calls.SkipWhile(call => call != rule).Skip(1);
        var interveningValidator = false;
        foreach (var call in following)
        {
            var name = InvocationChain.NameOf(call);
            if (_conditions.Contains(name))
            {
                var argument = call.ArgumentList.Arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "applyConditionTo") ??
                    call.ArgumentList.Arguments.Skip(1).FirstOrDefault(argument => argument.NameColon is null);
                var currentOnly = argument is not null && semanticModel.GetConstantValue(argument.Expression) is { HasValue: true, Value: 1 };
                if (!currentOnly || !interveningValidator)
                {
                    return $"'{name}{call.ArgumentList}'";
                }
            }
            else if (name != WithMessage && !_modifiers.Contains(name))
            {
                interveningValidator = true;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads one call of a rule chain.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <param name="property">The property the chain declares rules for.</param>
    /// <param name="forEach">Whether the rules were declared for each element of a collection.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <param name="rules">The rules collected so far.</param>
    /// <param name="preceding">The number of rules the call before this one declared.</param>
    /// <param name="conditional">Whether a named predicate in the chain executes conditionally.</param>
    /// <param name="condition">The condition holding this declarative rule, if any.</param>
    /// <returns>The number of rules the call added, or minus one for an omitted validator.</returns>
    int ReadCall(
        InvocationExpressionSyntax call,
        string property,
        bool forEach,
        SemanticModel semanticModel,
        string location,
        IList<ValidationRuleModel> rules,
        int preceding,
        bool conditional,
        string? condition)
    {
        var name = InvocationChain.NameOf(call);

        if (string.Equals(name, WithMessage, StringComparison.Ordinal))
        {
            ApplyMessage(call, semanticModel, rules, preceding, location);

            return 0;
        }

        if (_conditions.Contains(name))
        {
            return 0;
        }

        if (condition is not null && ((!forEach && name == Length && call.ArgumentList.Arguments.Count == 2) ||
            ValidationRuleKinds.TryResolve(name, forEach, out _)))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The '{name}' rule on '{property}' executes conditionally under {condition}, so it was left out rather than stated unconditionally",
                location);

            return -1;
        }

        if (!forEach && string.Equals(name, Length, StringComparison.Ordinal) && call.ArgumentList.Arguments.Count == 2)
        {
            rules.Add(new(property, ValidationRuleKind.Min, _operands.Constant(call, 0, semanticModel, location), null));
            rules.Add(new(property, ValidationRuleKind.Max, _operands.Constant(call, 1, semanticModel, location), null));

            return 2;
        }

        if (!forEach && !property.Contains('.', StringComparison.Ordinal) && name == "Must" &&
            call.ArgumentList.Arguments is [var argument] &&
            argument.Expression is IdentifierNameSyntax or MemberAccessExpressionSyntax &&
            semanticModel.GetSymbolInfo(argument.Expression).Symbol is IMethodSymbol predicate &&
            predicate.SourceFilePath() is { } source && !GeneratedSource.Is(source) &&
            paths?.Relative(source) is { } path && !Path.IsPathRooted(path) && !path.Contains(':', StringComparison.Ordinal))
        {
            if (conditional)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableValidationRule,
                    $"The named rule '{predicate.Name}' on '{property}' executes conditionally, but a named rule carries no condition, so it was left out",
                    location);

                return -1;
            }

            rules.Add(new(property, ValidationRuleKind.Rule, predicate.Name, null) { SourceFilePath = path });

            return 1;
        }

        if (_modifiers.Contains(name))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The '{name}' modifier on '{property}' has no declarative counterpart; it adds no validator to this chain, so it does not change which rule a following message belongs to",
                location);

            return 0;
        }

        if (!ValidationRuleKinds.TryResolve(name, forEach, out var kind))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The '{name}' rule on '{property}' lives in code and has no declarative counterpart, so it was left out",
                location);

            return -1;
        }

        rules.Add(new(property, kind, _operands.Read(call, name, semanticModel, location), null));

        return 1;
    }

    /// <summary>
    /// Applies a message to every rule the call before it declared.
    /// </summary>
    /// <param name="call">The call carrying the message.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="rules">The rules collected so far.</param>
    /// <param name="preceding">The number of rules the call before this one declared.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <remarks>
    /// One call can declare more than one rule - a length range is a lower bound and an upper bound - and a message
    /// written after it was written about the range rather than about its upper half. Attaching it to the last rule
    /// alone would leave the lower bound reporting a message the developer never wrote.
    /// </remarks>
    void ApplyMessage(
        InvocationExpressionSyntax call,
        SemanticModel semanticModel,
        IList<ValidationRuleModel> rules,
        int preceding,
        string location)
    {
        var argument = InvocationChain.ArgumentOf(call);
        var message = ValidationMessages.Read(argument, semanticModel);

        if (message is null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The message '{argument}' is put together while the request runs rather than written down, so the rule it belongs to states none",
                location);

            return;
        }

        if (preceding == 0)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The message '{message}' follows nothing the document states a rule for, so there is nothing to attach it to and it was left out",
                location);

            return;
        }

        for (var index = rules.Count - preceding; index < rules.Count; index++)
        {
            rules[index] = rules[index] with { Message = message };
        }
    }
}
