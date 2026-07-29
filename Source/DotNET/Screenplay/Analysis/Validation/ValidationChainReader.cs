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
    /// The call constraining a value to look like an email address.
    /// </summary>
    public const string EmailAddress = "EmailAddress";

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
    /// Reads the expression a message is carried by, unwrapping the lambda form <c>WithMessage</c> is idiomatically
    /// given.
    /// </summary>
    /// <param name="argument">The argument the message was declared with.</param>
    /// <returns>The expression whose constant value is the message.</returns>
    /// <remarks>
    /// FluentValidation lets a message be a value or a lambda producing one, and the lambda form pointing at a
    /// message constant - <c>WithMessage(_ =&gt; Messages.NameRequired)</c> - is the common way an application keeps its
    /// messages in one place. The lambda body is a plain reference the semantic model reads a compile-time constant
    /// from, so the body is what the constant is asked of; a message computed at runtime - an interpolation, a call,
    /// a culture-dependent lookup - has no constant to read and is left out as before.
    /// </remarks>
    static ExpressionSyntax MessageExpression(ExpressionSyntax argument) => argument switch
    {
        SimpleLambdaExpressionSyntax { ExpressionBody: { } body } => body,
        ParenthesizedLambdaExpressionSyntax { ExpressionBody: { } body } => body,
        _ => argument
    };

    /// <summary>
    /// Reads the operand a rule compares against.
    /// </summary>
    /// <param name="call">The call declaring the rule.</param>
    /// <param name="name">The name of the rule builder.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <returns>The operand, or <see langword="null"/> when the rule takes none.</returns>
    object? OperandOf(InvocationExpressionSyntax call, string name, SemanticModel semanticModel, string location) =>
        string.Equals(name, EmailAddress, StringComparison.Ordinal)
            ? ValidationRuleKinds.EmailPattern
            : Constant(call, 0, semanticModel, location);

    /// <summary>
    /// Reads the constant value of an argument.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <param name="index">The position of the argument.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <returns>The value, or <see langword="null"/> when the argument is not a constant.</returns>
    /// <remarks>
    /// A rule comparing against a member of an enumeration is given the number behind the member, which would have
    /// the document compare a concept declaring names against a number. Naming the member is what keeps the rule
    /// readable against the concept it is written about.
    /// </remarks>
    object? Constant(InvocationExpressionSyntax call, int index, SemanticModel semanticModel, string location)
    {
        var argument = InvocationChain.ArgumentOf(call, index);
        if (argument is null)
        {
            return null;
        }

        var value = semanticModel.GetConstantValue(argument).Value;
        if (EnumConstants.EnumerationOf(argument, semanticModel) is not { } enumeration)
        {
            return value;
        }

        if (EnumConstants.TryResolve(enumeration, value, out var member))
        {
            return member;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnnamedEnumerationValue,
            $"'{enumeration.Name}' declares no member with the value '{value}', so it is written as that number rather than as a name the concept declares",
            location);

        return value;
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

        if (!forEach && string.Equals(name, Length, StringComparison.Ordinal) && call.ArgumentList.Arguments.Count == 2)
        {
            rules.Add(new(property, ValidationRuleKind.Min, Constant(call, 0, semanticModel, location), null));
            rules.Add(new(property, ValidationRuleKind.Max, Constant(call, 1, semanticModel, location), null));

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

        rules.Add(new(property, kind, OperandOf(call, name, semanticModel, location), null));

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
        var message = InvocationChain.ArgumentOf(call) is { } argument
            ? semanticModel.GetConstantValue(MessageExpression(argument)).Value as string
            : null;
        if (message is null || preceding == 0)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableValidationRule,
                "A message was declared without a constant value or without a rule preceding it, and was left out",
                location);

            return;
        }

        for (var index = rules.Count - preceding; index < rules.Count; index++)
        {
            rules[index] = rules[index] with { Message = message };
        }
    }
}
