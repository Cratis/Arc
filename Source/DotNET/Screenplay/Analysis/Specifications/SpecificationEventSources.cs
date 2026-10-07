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
    readonly List<(ISymbol? Symbol, ISymbol? Receiver, LiteralSource? Literal)> _sources = [];
    readonly Dictionary<Compilation, HeldValues> _held = [];

    /// <summary>
    /// Reads a source from an append, assertion, or fluent event-source builder.
    /// </summary>
    /// <param name="invocation">The call stating an occurrence.</param>
    /// <param name="method">The resolved method.</param>
    /// <param name="semanticModel">The model resolving the source expression.</param>
    /// <param name="draft">The scenario collecting the occurrences.</param>
    /// <param name="requireConcrete">Whether an explicit source must be stated rather than shared symbolically.</param>
    /// <returns>The concrete source, or null for a shared symbolic source.</returns>
    public LiteralSource? Read(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel semanticModel, SpecificationDraft draft, bool requireConcrete = false)
    {
        var expression = CallArguments.For(invocation, method, "eventSourceId").SingleOrDefault();
        if (expression is null)
        {
            var builder = invocation.Expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(call => semanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol candidate &&
                    (candidate.ReturnType.Is(WellKnownTypeNames.EventSourceGivenBuilder) ||
                     candidate.ReturnType.Is(WellKnownTypeNames.CommandScenarioSourceGivenBuilder) ||
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
        if (!_held.TryGetValue(semanticModel.Compilation, out var held))
        {
            _held[semanticModel.Compilation] = held = new(new SemanticModels([semanticModel.Compilation]));
        }

        ISymbol? receiver = null;
        if (symbol is { IsStatic: false } && expression is MemberAccessExpressionSyntax access &&
            MappingSourceReader.Unwrap(access.Expression) is not ThisExpressionSyntax)
        {
            var target = MappingSourceReader.Unwrap(access.Expression);
            receiver = target is IdentifierNameSyntax ? semanticModel.GetSymbolInfo(target).Symbol : null;
            if (receiver is null || !held.IsStable(receiver, semanticModel.Compilation))
            {
                draft.CannotRead("its event source uses an instance receiver that is not provably stable");
                symbol = null;
            }
        }

        if (symbol is not (IFieldSymbol or ILocalSymbol or IPropertySymbol))
        {
            symbol = null;
        }
        else if (!held.IsStable(symbol, semanticModel.Compilation))
        {
            draft.CannotRead($"its event source '{symbol.Name}' is reassigned or has a computed getter, so repeated references do not prove the same value");
            symbol = null;
        }

        if (_sources.Exists(source => !(literal is not null && source.Literal is not null) &&
            !(symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, source.Symbol) &&
                SymbolEqualityComparer.Default.Equals(receiver, source.Receiver))))
        {
            draft.CannotRead("its event sources are not provably the same and cannot be stated as concrete for values");
        }

        _sources.Add((symbol, receiver, literal));
        if (requireConcrete && literal is null)
        {
            draft.CannotRead("its explicit given event source cannot be stated as a concrete for value beside the command's destination");
        }

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
