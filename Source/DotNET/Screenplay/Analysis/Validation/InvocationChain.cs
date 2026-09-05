// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Represents a fluent call chain, unwound so that it reads in the order it was written.
/// </summary>
/// <param name="Root">The call the chain starts from.</param>
/// <param name="RootName">The name of the call the chain starts from.</param>
/// <param name="Calls">Everything called on the result, in source order.</param>
public record InvocationChain(
    InvocationExpressionSyntax Root,
    string RootName,
    IReadOnlyList<InvocationExpressionSyntax> Calls)
{
    /// <summary>
    /// Unwinds a fluent call chain.
    /// </summary>
    /// <param name="expression">The outermost call of the chain.</param>
    /// <returns>The chain, or <see langword="null"/> when the expression is not a chain of calls.</returns>
    /// <remarks>
    /// A chain is written outermost last but read innermost first, so unwinding it is what lets a reader of the
    /// analysis follow the same order the developer wrote.
    /// </remarks>
    public static InvocationChain? Unwind(ExpressionSyntax? expression)
    {
        var calls = new List<InvocationExpressionSyntax>();
        var current = expression;

        while (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation)
        {
            calls.Insert(0, invocation);
            current = member.Expression;
        }

        return current is InvocationExpressionSyntax { Expression: SimpleNameSyntax name } root
            ? new(root, name.Identifier.ValueText, calls)
            : null;
    }

    /// <summary>
    /// Unwinds a fluent call chain that may start from something other than a call.
    /// </summary>
    /// <param name="expression">The outermost call of the chain.</param>
    /// <returns>Every call of the chain, in source order.</returns>
    /// <remarks>
    /// A builder chain starts from the builder itself rather than from a call, which is the difference between this
    /// and <see cref="Unwind"/> - here there is no root call to name.
    /// </remarks>
    public static IReadOnlyList<InvocationExpressionSyntax> Sequence(ExpressionSyntax? expression)
    {
        var calls = new List<InvocationExpressionSyntax>();
        var current = expression;

        while (current is InvocationExpressionSyntax invocation)
        {
            calls.Insert(0, invocation);
            current = invocation.Expression is MemberAccessExpressionSyntax member ? member.Expression : null;
        }

        return calls;
    }

    /// <summary>
    /// Gets the name of a call within the chain.
    /// </summary>
    /// <param name="invocation">The call to name.</param>
    /// <returns>The name.</returns>
    public static string NameOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax member ? member.Name.Identifier.ValueText : string.Empty;

    /// <summary>
    /// Gets the type argument a generic call was given.
    /// </summary>
    /// <param name="invocation">The call to read.</param>
    /// <returns>The type argument, or <see langword="null"/> when the call has none.</returns>
    public static TypeSyntax? TypeArgumentOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } => generic.TypeArgumentList.Arguments.FirstOrDefault(),
            GenericNameSyntax generic => generic.TypeArgumentList.Arguments.FirstOrDefault(),
            _ => null
        };

    /// <summary>
    /// Gets the argument at a position of a call.
    /// </summary>
    /// <param name="invocation">The call to read.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The argument, or <see langword="null"/> when the call was not given one.</returns>
    public static ExpressionSyntax? ArgumentOf(InvocationExpressionSyntax invocation, int index = 0) =>
        invocation.ArgumentList.Arguments.Count > index ? invocation.ArgumentList.Arguments[index].Expression : null;
}
