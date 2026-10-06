// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Retains concrete occurrence sources and prevents unrelated computed sources from collapsing into one.
/// </summary>
internal class SpecificationEventSources
{
    readonly List<(ISymbol? Symbol, LiteralSource? Literal)> _sources = [];

    /// <summary>
    /// Reads a source from an append, assertion, or fluent event-source builder.
    /// </summary>
    /// <param name="invocation">The call stating an occurrence.</param>
    /// <param name="method">The resolved method.</param>
    /// <param name="semanticModel">The model resolving the source expression.</param>
    /// <param name="draft">The scenario collecting the occurrences.</param>
    /// <returns>The concrete source, or null for a shared symbolic source.</returns>
    public LiteralSource? Read(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel semanticModel, SpecificationDraft draft)
    {
        var expression = CallArguments.For(invocation, method, "eventSourceId").SingleOrDefault();
        if (expression is null)
        {
            var builder = invocation.Expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(call => semanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol candidate &&
                    (candidate.ReturnType.Is(WellKnownTypeNames.EventSourceGivenBuilder) ||
                     candidate.ReturnType.Is(WellKnownTypeNames.EventSourceWhenBuilder)));
            if (builder is not null && semanticModel.GetSymbolInfo(builder).Symbol is IMethodSymbol builderMethod)
            {
                expression = CallArguments.For(builder, builderMethod, "eventSourceId").SingleOrDefault();
            }
        }

        if (expression is null)
        {
            return null;
        }

        expression = MappingSourceReader.Unwrap(expression);
        var literal = LiteralOf(expression, semanticModel);
        var symbol = expression is IdentifierNameSyntax or MemberAccessExpressionSyntax
            ? semanticModel.GetSymbolInfo(expression).Symbol
            : null;
        if (symbol is not (IFieldSymbol or ILocalSymbol or IPropertySymbol or IParameterSymbol))
        {
            symbol = null;
        }

        if (_sources.Exists(source => !(literal is not null && source.Literal is not null) &&
            !(symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, source.Symbol))))
        {
            draft.CannotRead("its event sources are not provably the same and cannot be stated as concrete for values");
        }

        _sources.Add((symbol, literal));

        return literal;
    }

    static LiteralSource? LiteralOf(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument] } &&
            semanticModel.GetTypeInfo(expression).Type.Is(WellKnownTypeNames.EventSourceId))
        {
            expression = MappingSourceReader.Unwrap(argument.Expression);
        }

        var constant = semanticModel.GetConstantValue(expression);

        return constant is { HasValue: true, Value: string or bool or int or long or float or double or decimal }
            ? new(constant.Value)
            : null;
    }
}
