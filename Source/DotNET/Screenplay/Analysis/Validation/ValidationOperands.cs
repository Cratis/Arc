// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Reads the value a validation rule compares against.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A rule compares against one of two things, and both are written down in full. A value the compiler holds is the
/// obvious one. The other is another property of the same command - an end date is on or after the start date it was
/// sent with - which is a lambda naming that property and nothing more, and which a rule operand carries as the path
/// it names.
/// </remarks>
public class ValidationOperands(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The call constraining a value to look like an email address.
    /// </summary>
    public const string EmailAddress = "EmailAddress";

    /// <summary>
    /// Reads the operand a rule compares against.
    /// </summary>
    /// <param name="call">The call declaring the rule.</param>
    /// <param name="name">The name of the rule builder.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <returns>The operand, or <see langword="null"/> when the rule takes none that can be read.</returns>
    /// <remarks>
    /// A lambda is asked about before a constant is, because a lambda holds no constant and the question would only
    /// ever be answered with nothing. The other way round would drop every comparison written against a sibling
    /// property as though the rule had been given no operand at all.
    /// </remarks>
    public object? Read(InvocationExpressionSyntax call, string name, SemanticModel semanticModel, string location)
    {
        if (string.Equals(name, EmailAddress, StringComparison.Ordinal))
        {
            return ValidationRuleKinds.EmailPattern;
        }

        return LambdaPaths.Read(InvocationChain.ArgumentOf(call)) is { } path
            ? new PropertyPathSource(path)
            : Constant(call, 0, semanticModel, location);
    }

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
    public object? Constant(InvocationExpressionSyntax call, int index, SemanticModel semanticModel, string location)
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
}
