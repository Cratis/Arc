// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Reads the constant values a policy requirement is given.
/// </summary>
/// <remarks>
/// A requirement takes its values either one by one or as a collection, and both forms mean the same thing. Only
/// constants are read - a value computed at startup is not something a document can state.
/// </remarks>
public static class PolicyValues
{
    /// <summary>
    /// Reads the constant strings a run of arguments yields.
    /// </summary>
    /// <param name="arguments">The arguments to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the arguments live in.</param>
    /// <returns>The values, or <see langword="null"/> when any argument is not made of constants.</returns>
    public static IReadOnlyList<string>? Of(IEnumerable<ExpressionSyntax> arguments, SemanticModel semanticModel)
    {
        var values = new List<string>();

        foreach (var argument in arguments)
        {
            if (!Collect(argument, semanticModel, values))
            {
                return null;
            }
        }

        return values;
    }

    /// <summary>
    /// Collects the constant strings one argument yields.
    /// </summary>
    /// <param name="expression">The argument to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the argument lives in.</param>
    /// <param name="values">The values collected so far.</param>
    /// <returns>True when the argument is made of constants.</returns>
    static bool Collect(ExpressionSyntax expression, SemanticModel semanticModel, List<string> values)
    {
        if (semanticModel.GetConstantValue(expression).Value is string constant)
        {
            values.Add(constant);

            return true;
        }

        return Elements(expression) is { } elements && elements.All(_ => Collect(_, semanticModel, values));
    }

    /// <summary>
    /// Gets the elements a collection of values is written as.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <returns>The elements, or <see langword="null"/> when the expression is not a collection of values.</returns>
    static IEnumerable<ExpressionSyntax>? Elements(ExpressionSyntax expression) => expression switch
    {
        ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
        ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
        CollectionExpressionSyntax collection => collection.Elements.OfType<ExpressionElementSyntax>().Select(_ => _.Expression),
        _ => null
    };
}
