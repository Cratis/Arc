// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Gets the expressions a call gives to one of its parameters.
/// </summary>
/// <remarks>
/// What a specification states sits in one argument of a call, and which one it is has to be resolved by name rather
/// than by position - the calls carrying it declare everything after it as optional, take it through a parameter
/// array, or are extension methods whose first parameter is the receiver. A parameter array and a parameter taking a
/// collection are both flattened, so that stating three events in one call and stating them in three reads the same
/// way.
/// </remarks>
public static class CallArguments
{
    /// <summary>
    /// Gets the expressions a call gives to a parameter.
    /// </summary>
    /// <param name="invocation">The call to read.</param>
    /// <param name="method">The method being called.</param>
    /// <param name="parameter">The name of the parameter to read.</param>
    /// <returns>The expressions, in the order the call declares them.</returns>
    public static IEnumerable<ExpressionSyntax> For(InvocationExpressionSyntax invocation, IMethodSymbol method, string parameter)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var given = new List<ExpressionSyntax>();

        for (var index = 0; index < arguments.Count; index++)
        {
            if (string.Equals(NameOf(arguments[index], method, index), parameter, StringComparison.Ordinal))
            {
                given.AddRange(Flatten(arguments[index].Expression));
            }
        }

        return given;
    }

    /// <summary>
    /// Gets the name of the parameter an argument fills in.
    /// </summary>
    /// <param name="argument">The argument to name.</param>
    /// <param name="method">The method being called.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The parameter name, or <see langword="null"/> when it cannot be resolved.</returns>
    static string? NameOf(ArgumentSyntax argument, IMethodSymbol method, int index)
    {
        if (argument.NameColon is { } named)
        {
            return named.Name.Identifier.ValueText;
        }

        if (index < method.Parameters.Length)
        {
            return method.Parameters[index].Name;
        }

        return method.Parameters is [.., { IsParams: true } last] ? last.Name : null;
    }

    /// <summary>
    /// Opens up an expression that holds several values into the values it holds.
    /// </summary>
    /// <param name="expression">The expression to open up.</param>
    /// <returns>The values, or the expression itself when it holds one.</returns>
    static IEnumerable<ExpressionSyntax> Flatten(ExpressionSyntax expression) => expression switch
    {
        CollectionExpressionSyntax collection => collection.Elements.Select(ValueOf),
        ImplicitArrayCreationExpressionSyntax implicitly => implicitly.Initializer.Expressions,
        ArrayCreationExpressionSyntax array when array.Initializer is { } initializer => initializer.Expressions,
        _ => [expression]
    };

    /// <summary>
    /// Gets the expression one element of a collection stands for.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <returns>The expression.</returns>
    /// <remarks>
    /// An element spreading another collection into this one stands for however many values that collection holds,
    /// which is not something a source text says. The expression being spread is returned as it is, so that whoever
    /// asked for the values finds one it cannot read rather than a list quietly missing them.
    /// </remarks>
    static ExpressionSyntax ValueOf(CollectionElementSyntax element) => element switch
    {
        ExpressionElementSyntax value => value.Expression,
        SpreadElementSyntax spread => spread.Expression,
        _ => SyntaxFactory.IdentifierName(element.ToString())
    };
}
