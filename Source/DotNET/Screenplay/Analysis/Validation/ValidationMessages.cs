// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Reads what a <c>WithMessage</c> call says.
/// </summary>
/// <remarks>
/// A message is written in two shapes that say the same thing. Given directly it is text the compiler already holds.
/// Given as a lambda it is usually a reference to a class the application declares its messages in once and names from
/// every validator, which is how a message is written wherever messages are shared rather than repeated - and the
/// lambda there computes nothing at all, so refusing it left the document without a single message it could have
/// stated.
/// <para>
/// The lambda form exists so that a message can be built from the value being validated, and a message built while
/// the request runs has no text to write down. So the compiler is asked for the text, and where it holds none the
/// source is asked for a key: an application showing its messages in more than one language writes each of them as a
/// lookup rather than as a value, and the key it looks up is the one thing about that message which is the same in
/// every language. The key is what the document states, because stating text would settle it on a language the
/// application itself never settled on.
/// </para>
/// <para>
/// What neither answers for is a message genuinely put together while the request runs, which stays reported.
/// </para>
/// </remarks>
public static class ValidationMessages
{
    /// <summary>
    /// Reads the message an argument carries.
    /// </summary>
    /// <param name="argument">The argument to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the argument lives in.</param>
    /// <returns>The message, or <see langword="null"/> when the argument says nothing that can be written down.</returns>
    public static string? Read(ExpressionSyntax? argument, SemanticModel semanticModel) =>
        argument is null ? null : MessageOf(argument, semanticModel) ?? MessageOf(BodyOf(argument), semanticModel);

    /// <summary>
    /// Gets what an expression says - the text when the compiler holds it, and the key it is looked up by when not.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>The message, or <see langword="null"/> when the expression says nothing that can be written down.</returns>
    static string? MessageOf(ExpressionSyntax? expression, SemanticModel semanticModel) =>
        TextOf(expression, semanticModel) ?? KeyOf(expression, semanticModel);

    /// <summary>
    /// Gets the key an expression is looked up by, in the form the document references a localized string in.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>The reference, or <see langword="null"/> when there is no key the document can write.</returns>
    /// <remarks>
    /// A key the document has no way of writing is left to be reported rather than written some other way. The
    /// language references a localized string by a path of bare words while a resource key is free of that - it is a
    /// name in the resource and nowhere else, which is why a build turning one into a property already has to rename
    /// it. Making that same alteration here would name a key the resource does not hold, and a rule stating a message
    /// that resolves to nothing is worse than one stating no message at all.
    /// </remarks>
    static string? KeyOf(ExpressionSyntax? expression, SemanticModel semanticModel) =>
        ResourceKeys.Read(expression, semanticModel) is { } key && IsWritable(key)
            ? $"{ScreenplayIdentifier.LocalizationPrefix}{key}"
            : null;

    /// <summary>
    /// Determines whether every part of a key can be written to the document as a bare word.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True when the key can be written.</returns>
    static bool IsWritable(string key) =>
        Array.TrueForAll(key.Split('.'), ScreenplayIdentifier.IsBareIdentifier);

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
