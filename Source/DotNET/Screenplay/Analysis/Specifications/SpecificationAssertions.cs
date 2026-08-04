// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Recognizes what an assertion of a specification says the outcome was.
/// </summary>
/// <remarks>
/// Assertions are matched on the name of the helper alone. The same helper is declared several times over across the
/// testing packages - once for the result of a command, once for the result of an append, once for the scenario
/// itself - and a specification calls whichever of them its own shape reaches, so matching the declaring type would
/// recognize the same sentence in one specification and not in the next.
/// <para>
/// Only the two outcomes Screenplay can hold are recognized: an event was appended, and the command was rejected.
/// Everything else a specification asserts - how far the sequence got, that no exception was thrown, that the result
/// was valid - says nothing the language has a place for and is passed over rather than reported, in the same way a
/// unit level specification is.
/// </para>
/// </remarks>
public static class SpecificationAssertions
{
    /// <summary>The assertion naming an event the command appended.</summary>
    public const string AppendedEventAssertion = "ShouldHaveAppendedEvent";

    /// <summary>The assertion saying a value is false, which is a rejection when the value is the success of a result.</summary>
    public const string FalseAssertion = "ShouldBeFalse";

    /// <summary>The value of a result saying whether the command was carried out.</summary>
    public const string SuccessProperty = "IsSuccess";

    /// <summary>The assertions saying the command was rejected, without naming why.</summary>
    public static readonly string[] Rejections =
    [
        "ShouldHaveExceptions",
        "ShouldHaveValidationErrors",
        "ShouldNotBeAuthorized",
        "ShouldNotBeSuccessful"
    ];

    /// <summary>The assertions saying the command was rejected and naming the reason.</summary>
    public static readonly string[] NamedRejections =
    [
        "ShouldHaveConstraintViolation",
        "ShouldHaveConstraintViolationFor",
        "ShouldHaveValidationErrorBecauseOf",
        "ShouldHaveValidationErrorFor"
    ];

    /// <summary>
    /// Gets the event an assertion says was appended.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>The event type, or <see langword="null"/> when the assertion is about something else.</returns>
    /// <remarks>
    /// The event is the last type argument rather than the only one, because the helper taking the scenario also
    /// takes the command it is a scenario for, which has to be named ahead of it.
    /// </remarks>
    public static ITypeSymbol? AppendedEventOf(IMethodSymbol method) =>
        string.Equals(method.Name, AppendedEventAssertion, StringComparison.Ordinal)
            ? method.TypeArguments.LastOrDefault()
            : null;

    /// <summary>
    /// Determines whether an assertion says the command was rejected.
    /// </summary>
    /// <param name="invocation">The assertion to read.</param>
    /// <param name="method">The method being called.</param>
    /// <returns>True when the assertion is a rejection.</returns>
    public static bool IsRejection(InvocationExpressionSyntax invocation, IMethodSymbol method) =>
        Array.Exists(Rejections, _ => string.Equals(_, method.Name, StringComparison.Ordinal)) ||
        IsNamedRejection(method) ||
        IsUnsuccessful(invocation, method);

    /// <summary>
    /// Determines whether an assertion names the reason the command was rejected.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when the assertion names a reason.</returns>
    public static bool IsNamedRejection(IMethodSymbol method) =>
        Array.Exists(NamedRejections, _ => string.Equals(_, method.Name, StringComparison.Ordinal));

    /// <summary>
    /// Determines whether an assertion says the result of the command was not a success.
    /// </summary>
    /// <param name="invocation">The assertion to read.</param>
    /// <param name="method">The method being called.</param>
    /// <returns>True when the assertion is about the success of a result.</returns>
    static bool IsUnsuccessful(InvocationExpressionSyntax invocation, IMethodSymbol method) =>
        string.Equals(method.Name, FalseAssertion, StringComparison.Ordinal) &&
        invocation.Expression is MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax subject } &&
        string.Equals(subject.Name.Identifier.ValueText, SuccessProperty, StringComparison.Ordinal);
}
