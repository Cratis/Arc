// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Aggregates;

/// <summary>
/// Holds what each parameter of a method was given at the call site it was reached through.
/// </summary>
/// <remarks>
/// A command that hands its work to an aggregate root passes its own input along, and the aggregate root names that
/// input whatever it likes. Without the substitution the call site declares, every value inside the aggregate root
/// would look like code rather than like the command input it really is.
/// </remarks>
public sealed class ParameterBindings
{
    readonly Dictionary<ISymbol, BoundArgument> _bound;

    ParameterBindings(Dictionary<ISymbol, BoundArgument> bound) => _bound = bound;

    /// <summary>
    /// Binds the parameters of a method to what one call site gave them.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <param name="call">The call site.</param>
    /// <param name="semanticModel">The semantic model of the tree the call site lives in.</param>
    /// <returns>The <see cref="ParameterBindings"/>.</returns>
    public static ParameterBindings For(IMethodSymbol method, InvocationExpressionSyntax call, SemanticModel semanticModel)
    {
        var bound = new Dictionary<ISymbol, BoundArgument>(SymbolEqualityComparer.Default);
        var arguments = call.ArgumentList.Arguments;

        for (var index = 0; index < arguments.Count; index++)
        {
            if (ParameterOf(method, arguments[index], index) is { } parameter)
            {
                bound[parameter] = new(arguments[index].Expression, semanticModel);
            }
        }

        return new(bound);
    }

    /// <summary>
    /// Resolves what an expression was given, when it names a bound parameter and nothing else.
    /// </summary>
    /// <param name="expression">The expression to resolve.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>The binding, or <see langword="null"/> when the expression names no bound parameter.</returns>
    public BoundArgument? Resolve(ExpressionSyntax expression, SemanticModel semanticModel) =>
        expression is IdentifierNameSyntax identifier &&
        semanticModel.GetSymbolInfo(identifier).Symbol is IParameterSymbol parameter &&
        _bound.TryGetValue(parameter, out var argument)
            ? argument
            : null;

    /// <summary>
    /// Gets the parameter an argument fills in.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <param name="argument">The argument.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The parameter, or <see langword="null"/> when it cannot be resolved.</returns>
    static IParameterSymbol? ParameterOf(IMethodSymbol method, ArgumentSyntax argument, int index)
    {
        if (argument.NameColon is { } named)
        {
            return method.Parameters.FirstOrDefault(_ => string.Equals(_.Name, named.Name.Identifier.ValueText, StringComparison.Ordinal));
        }

        return index < method.Parameters.Length ? method.Parameters[index] : null;
    }
}
