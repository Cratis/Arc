// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Reads the text a <c>WithMessage</c> call carries.
/// </summary>
/// <remarks>
/// A message is written in two shapes that say the same thing. Given directly it is text the compiler already holds.
/// Given as a lambda it is usually a reference to a class the application declares its messages in once and names from
/// every validator, which is how a message is written wherever messages are shared rather than repeated - and the
/// lambda there computes nothing at all, so refusing it left the document without a single message it could have
/// stated.
/// <para>
/// The lambda form exists so that a message can be built from the value being validated, and a message built while
/// the request runs has no text to write down - text put together from the value, or looked up in a resource by a
/// key resolved against the culture of the caller. What the compiler hands over is exactly the line between the two,
/// which is why it is what is asked rather than the shape of the expression.
/// </para>
/// </remarks>
public static class ValidationMessages
{
    /// <summary>
    /// Reads the message an argument carries.
    /// </summary>
    /// <param name="argument">The argument to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the argument lives in.</param>
    /// <returns>The message, or <see langword="null"/> when the argument carries no text the compiler holds.</returns>
    public static string? Read(ExpressionSyntax? argument, SemanticModel semanticModel) =>
        argument is null ? null : TextOf(argument, semanticModel) ?? TextOf(BodyOf(argument), semanticModel);

    /// <summary>
    /// Gets the expression a lambda returns.
    /// </summary>
    /// <param name="argument">The argument to read.</param>
    /// <returns>The expression, or <see langword="null"/> when the argument is not a lambda returning one.</returns>
    /// <remarks>
    /// A lambda with a block body runs statements to arrive at its message, which is the definition of a message
    /// there is no text for.
    /// </remarks>
    static ExpressionSyntax? BodyOf(ExpressionSyntax argument) => argument switch
    {
        SimpleLambdaExpressionSyntax simple => simple.ExpressionBody,
        ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ExpressionBody,
        _ => null
    };

    /// <summary>
    /// Gets the text an expression holds, when the compiler holds it.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>The text, or <see langword="null"/> when the expression is not text known while compiling.</returns>
    /// <remarks>
    /// A reference to something declared as a constant is answered here just as a literal is, which is what makes the
    /// shared message class work - the compiler substituted the text at the point of use before anything was asked of
    /// it. A field holding text it was only assigned once is not that, and neither is a property returning one.
    /// </remarks>
    static string? TextOf(ExpressionSyntax? expression, SemanticModel semanticModel) =>
        expression is null ? null : semanticModel.GetConstantValue(expression).Value as string;
}
