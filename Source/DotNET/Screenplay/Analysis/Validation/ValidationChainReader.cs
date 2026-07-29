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
/// <remarks>
/// A chain names a property once and then declares rule after rule on it, with messages attaching to whichever rule
/// they were written after. Counting what each call declared is what lets a message find the right rule.
/// </remarks>
public class ValidationChainReader(ScreenplayDiagnostics diagnostics)
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

    readonly ValidationOperands _operands = new(diagnostics);

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

        foreach (var call in chain.Calls)
        {
            var added = ReadCall(call, property, forEach, semanticModel, location, rules, preceding);
            if (added > 0)
            {
                preceding = added;
            }
        }
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
    /// <returns>The number of rules the call added.</returns>
    int ReadCall(
        InvocationExpressionSyntax call,
        string property,
        bool forEach,
        SemanticModel semanticModel,
        string location,
        IList<ValidationRuleModel> rules,
        int preceding)
    {
        var name = InvocationChain.NameOf(call);

        if (string.Equals(name, WithMessage, StringComparison.Ordinal))
        {
            ApplyMessage(call, semanticModel, rules, preceding, location);

            return 0;
        }

        if (string.Equals(name, When, StringComparison.Ordinal) || string.Equals(name, Unless, StringComparison.Ordinal))
        {
            ReportCondition(call, name, property, location);

            return 0;
        }

        if (!forEach && string.Equals(name, Length, StringComparison.Ordinal) && call.ArgumentList.Arguments.Count == 2)
        {
            rules.Add(new(property, ValidationRuleKind.Min, _operands.Constant(call, 0, semanticModel, location), null));
            rules.Add(new(property, ValidationRuleKind.Max, _operands.Constant(call, 1, semanticModel, location), null));

            return 2;
        }

        if (!ValidationRuleKinds.TryResolve(name, forEach, out var kind))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                $"The '{name}' rule on '{property}' lives in code and has no declarative counterpart, so it was left out",
                location);

            return 0;
        }

        rules.Add(new(property, kind, _operands.Read(call, name, semanticModel, location), null));

        return 1;
    }

    /// <summary>
    /// Reports the condition the rules declared before it are held to.
    /// </summary>
    /// <param name="call">The call carrying the condition.</param>
    /// <param name="name">The name of the call.</param>
    /// <param name="property">The property the chain declares rules for.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <remarks>
    /// A rule that only holds sometimes is stated as though it always holds, because a rule carries no condition of
    /// its own yet (Cratis/Screenplay#32). That is a real difference between the document and the application, so the
    /// condition is written into the report rather than only its absence - a reader who can see what the rule was
    /// held to can tell one that hardly ever applies from one that nearly always does, and a report naming only the
    /// call says neither.
    /// </remarks>
    void ReportCondition(InvocationExpressionSyntax call, string name, string property, string location) =>
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableValidationRule,
            $"The rules on '{property}' are held to '{name}{call.ArgumentList}', and a rule carries no condition, so they are stated as though nothing held them",
            location);

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
