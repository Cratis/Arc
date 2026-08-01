// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the property paths a projection names on the event side.
/// </summary>
/// <remarks>
/// Event properties are referenced in a projection body exactly as they are serialized, which is lower camel case.
/// The printer writes an expression verbatim, so the casing has to be right before it gets there - unlike a read
/// model property, which emission camel cases itself.
/// </remarks>
public static class ProjectionPaths
{
    static readonly ScreenplayNaming _naming = new();

    /// <summary>
    /// Reads the event property path a lambda selects.
    /// </summary>
    /// <param name="expression">The lambda to read.</param>
    /// <returns>The path, or <see langword="null"/> when the lambda does not simply select a property.</returns>
    public static string? Read(ExpressionSyntax? expression) => Convert(Validation.LambdaPaths.Read(expression));

    /// <summary>
    /// Converts a property path onto the casing a projection body references it by.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The path, or <see langword="null"/> when there is none.</returns>
    public static string? Convert(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : _naming.ToPropertyPath(path);

    /// <summary>
    /// Converts the name of an enumeration member onto the casing the concept declares it in.
    /// </summary>
    /// <param name="member">The name to convert.</param>
    /// <returns>The name.</returns>
    /// <remarks>
    /// A member is written into a projection body verbatim for the same reason an event property is, so it is cased
    /// here rather than at the printer. What comes out has to match the value the concept declaration carries, which
    /// emission writes through the same conversion.
    /// </remarks>
    public static string ConvertMember(string member) => _naming.ToPropertyName(member);

    /// <summary>
    /// Reads the read model property path a lambda selects, in the casing the model declares it.
    /// </summary>
    /// <param name="expression">The lambda to read.</param>
    /// <returns>The path, or <see langword="null"/> when the lambda does not simply select a property.</returns>
    public static string? ReadDeclared(ExpressionSyntax? expression) => Validation.LambdaPaths.Read(expression);

    /// <summary>
    /// Gets the calls of a chain that belong to one scope, never descending into a nested lambda.
    /// </summary>
    /// <param name="scope">The node holding the scope.</param>
    /// <returns>The chains, outermost call first.</returns>
    public static IEnumerable<InvocationExpressionSyntax> ChainsIn(SyntaxNode scope) =>
        scope.DescendantNodesAndSelf(_ => ReferenceEquals(_, scope) || _ is not AnonymousFunctionExpressionSyntax)
            .OfType<InvocationExpressionSyntax>()
            .Where(_ => _.Parent is not MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax });
}
