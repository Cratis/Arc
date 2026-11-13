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
        var subject = DateValidationRules.Subject(chain.Root, semanticModel);
        var site = new DateRuleSite(semanticModel, location, diagnostics);
        var scope = ValidationConditions.ScopeOf(chain, semanticModel);
        var conditional = scope is not null || chain.Calls.Any(call => _conditions.Contains(InvocationChain.NameOf(call)));

        foreach (var call in chain.Calls)
        {
            var condition = scope ?? ConditionFor(call, chain, semanticModel);
            if (DateValidationRules.Applies(InvocationChain.NameOf(call), subject, condition))
            {
                preceding = ReadDateRule(call, new(subject, property, forEach), site, rules);
                continue;
            }
            var added = ReadCall(call, new(property, forEach, semanticModel, location, rules, conditional), preceding, condition);
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
                if (!AppliesToCurrentOnly(call, semanticModel) || !interveningValidator)
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

    static bool AppliesToCurrentOnly(InvocationExpressionSyntax condition, SemanticModel semanticModel)
    {
        var argument = condition.ArgumentList.Arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "applyConditionTo") ??
            condition.ArgumentList.Arguments.Skip(1).FirstOrDefault(argument => argument.NameColon is null);
        return argument is not null && semanticModel.GetConstantValue(argument.Expression) is { HasValue: true, Value: 1 };
    }

    static bool IsLengthRange(InvocationExpressionSyntax call, bool forEach) =>
        !forEach && string.Equals(InvocationChain.NameOf(call), Length, StringComparison.Ordinal) && call.ArgumentList.Arguments.Count == 2;

    static bool IsDeclarativeRule(InvocationExpressionSyntax call, ValidationChainSite site) =>
        IsLengthRange(call, site.ForEach) || ValidationRuleKinds.TryResolve(InvocationChain.NameOf(call), site.ForEach, out _);

    static bool IsMust(InvocationExpressionSyntax call, ValidationChainSite site) =>
        !site.ForEach && InvocationChain.NameOf(call) == "Must";

    /// <summary>
    /// Reads one call of a rule chain.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <param name="site">What the chain's calls are read against.</param>
    /// <param name="preceding">The number of rules the call before this one declared.</param>
    /// <param name="condition">The condition holding this declarative rule, if any.</param>
    /// <returns>The number of rules the call added, or minus one for an omitted validator.</returns>
    int ReadCall(InvocationExpressionSyntax call, ValidationChainSite site, int preceding, string? condition)
    {
        var name = InvocationChain.NameOf(call);

        if (string.Equals(name, WithMessage, StringComparison.Ordinal))
        {
            ApplyMessage(call, site, preceding);

            return 0;
        }

        if (_conditions.Contains(name))
        {
            return 0;
        }

        return ReadConditionalRule(call, site, condition)
            ?? ReadLengthRange(call, site)
            ?? ReadNamedPredicate(call, site)
            ?? ReadDeclarativeRule(call, site);
    }

    int? ReadConditionalRule(InvocationExpressionSyntax call, ValidationChainSite site, string? condition)
    {
        if (condition is null || !IsDeclarativeRule(call, site))
        {
            return null;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableValidationRule,
            $"The '{InvocationChain.NameOf(call)}' rule on '{site.Property}' executes conditionally under {condition}, so it was left out rather than stated unconditionally",
            site.Location);

        return -1;
    }

    int? ReadLengthRange(InvocationExpressionSyntax call, ValidationChainSite site)
    {
        if (!IsLengthRange(call, site.ForEach))
        {
            return null;
        }

        site.Rules.Add(new(site.Property, ValidationRuleKind.Min, _operands.Constant(call, 0, site.SemanticModel, site.Location), null));
        site.Rules.Add(new(site.Property, ValidationRuleKind.Max, _operands.Constant(call, 1, site.SemanticModel, site.Location), null));

        return 2;
    }

    int? ReadNamedPredicate(InvocationExpressionSyntax call, ValidationChainSite site)
    {
        if (!IsMust(call, site) || ReadCustomRule(call, site.Property, site.SemanticModel, site.Conditional) is not { } custom)
        {
            return null;
        }

        if (site.Conditional)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The named rule '{custom.Value}' on '{site.Property}' executes conditionally, but a named rule carries no condition, so it was left out",
                site.Location);

            return -1;
        }

        site.Rules.Add(custom);

        return 1;
    }

    int ReadDeclarativeRule(InvocationExpressionSyntax call, ValidationChainSite site)
    {
        var name = InvocationChain.NameOf(call);
        if (_modifiers.Contains(name))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The '{name}' modifier on '{site.Property}' has no declarative counterpart; it adds no validator to this chain, so it does not change which rule a following message belongs to",
                site.Location);

            return 0;
        }

        if (!ValidationRuleKinds.TryResolve(name, site.ForEach, out var kind))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The '{name}' rule on '{site.Property}' lives in code and has no declarative counterpart, so it was left out",
                site.Location);

            return -1;
        }

        site.Rules.Add(new(site.Property, kind, _operands.Read(call, name, site.SemanticModel, site.Location), null));

        return 1;
    }

    ValidationRuleModel? ReadCustomRule(InvocationExpressionSyntax call, string property, SemanticModel model, bool conditional)
    {
        var checksum = !conditional && property == "Value" ? ChecksumValidation.Read(call, model) : null;

        return checksum is not null ? new(property, ValidationRuleKind.Checksum, checksum, null) : ReadNamedRule(call, property, model);
    }

    ValidationRuleModel? ReadNamedRule(InvocationExpressionSyntax call, string property, SemanticModel model)
    {
        if (property.Contains('.', StringComparison.Ordinal) || call.ArgumentList.Arguments is not [var argument])
        {
            return null;
        }
        if (argument.Expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax) || model.GetSymbolInfo(argument.Expression).Symbol is not IMethodSymbol predicate)
        {
            return null;
        }
        var path = PredicatePath(predicate);

        return path is null ? null : new(property, ValidationRuleKind.Rule, predicate.Name, null) { SourceFilePath = path };
    }

    string? PredicatePath(IMethodSymbol predicate)
    {
        if (predicate.SourceFilePath() is not { } source || GeneratedSource.Is(source))
        {
            return null;
        }
        var path = paths?.Relative(source);

        if (path is null)
        {
            return null;
        }
        if (Path.IsPathRooted(path))
        {
            return null;
        }

        return path.Contains(':', StringComparison.Ordinal) ? null : path;
    }

    /// <summary>
    /// Applies a message to every rule the call before it declared.
    /// </summary>
    /// <param name="call">The call carrying the message.</param>
    /// <param name="site">What the chain's calls are read against.</param>
    /// <param name="preceding">The number of rules the call before this one declared.</param>
    /// <remarks>
    /// One call can declare more than one rule - a length range is a lower bound and an upper bound - and a message
    /// written after it was written about the range rather than about its upper half. Attaching it to the last rule
    /// alone would leave the lower bound reporting a message the developer never wrote.
    /// </remarks>
    void ApplyMessage(InvocationExpressionSyntax call, ValidationChainSite site, int preceding)
    {
        var rules = site.Rules;
        var location = site.Location;
        var argument = InvocationChain.ArgumentOf(call);
        var message = ValidationMessages.Read(argument, site.SemanticModel);

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

    int ReadDateRule(InvocationExpressionSyntax call, DateRuleTarget target, DateRuleSite site, IList<ValidationRuleModel> rules)
    {
        if (DateValidationRules.Read(call, target, site) is not { } rule)
        {
            return 0;
        }
        rules.Add(rule);

        return 1;
    }
}
