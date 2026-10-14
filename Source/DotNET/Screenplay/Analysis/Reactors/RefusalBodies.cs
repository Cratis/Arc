// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Constraints;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Proves the whole callback is acknowledgment or an unconditional batch of event constructions.
/// </summary>
/// <param name="models">The models owning callback source.</param>
/// <param name="constraints">The batch equivalence catalog.</param>
internal class RefusalBodies(SemanticModels models, ConstraintCoverage? constraints)
{
    /// <summary>
    /// Reads a callback without evaluating arbitrary code or partially recovering its effects.
    /// </summary>
    /// <param name="callback">The lambda or method group.</param>
    /// <param name="model">The model resolving the callback.</param>
    /// <param name="handler">The reactor handler supplying captured trigger values.</param>
    /// <param name="constraint">Whether the selector admits constraint values.</param>
    /// <param name="routesAppends">Whether bare events inherit unsupported append routing.</param>
    /// <returns>The productions, empty for acknowledgment, or null for any unproven effect.</returns>
    internal IReadOnlyList<ProducesModel>? Read(ExpressionSyntax callback, SemanticModel model, IMethodSymbol handler, bool constraint, bool routesAppends)
    {
        if (Callback(callback, model) is not { } body || models.For(body.Body.SyntaxTree) is not { } bodyModel ||
            PureReturn(body.Body) is not { } returned)
        {
            return null;
        }
        if (returned.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return [];
        }

        return Productions(returned, bodyModel, handler, body.Refusal, constraint, routesAppends);
    }

    static (SyntaxNode Body, IParameterSymbol Refusal)? Callback(ExpressionSyntax expression, SemanticModel model)
    {
        if (expression is LambdaExpressionSyntax lambda && model.GetSymbolInfo(lambda).Symbol is IMethodSymbol { IsAsync: false, Parameters: [var parameter] })
        {
            return (lambda.Body, parameter);
        }
        if (expression is AnonymousMethodExpressionSyntax anonymous && model.GetSymbolInfo(anonymous).Symbol is IMethodSymbol { IsAsync: false, Parameters: [var argument] })
        {
            return (anonymous.Block, argument);
        }
        if (model.GetSymbolInfo(expression).Symbol is not IMethodSymbol { IsStatic: true, IsAsync: false, IsGenericMethod: false, Parameters: [var refusal] } method ||
            !refusal.Type.Is("Cratis.Arc.Commands.CommandRefusal") || HandlerBodies.Of(method).ToList() is not [var body])
        {
            return null;
        }

        // A static method group has no receiver evaluation or overridable dispatch.
        return (body, refusal);
    }

    static ExpressionSyntax? PureReturn(SyntaxNode body) => body switch
    {
        ExpressionSyntax expression => expression,
        BlockSyntax { Statements: [ReturnStatementSyntax { Expression: { } expression }] } => expression,
        _ => null
    };

    static ProducesModel? Production(ReactionEffect effect, SemanticModel model, IMethodSymbol handler, IParameterSymbol refusal, bool constraint)
    {
        var context = handler.Parameters.Length > 1 ? handler.Parameters[1] : null;
        var mappings = ReactionMappings.Read(effect.Creation, model, effect.Type, handler.Parameters[0], context, refusal, constraint);

        return mappings is null ? null : new(effect.Type.Name, null, mappings)
        {
            EventTypeIdentity = EventProducers.IdentityOf(effect.Type),
            Destination = effect.Destination
        };
    }

    List<ProducesModel>? Productions(ExpressionSyntax returned, SemanticModel model, IMethodSymbol handler, IParameterSymbol refusal, bool constraint, bool routesAppends)
    {
        var items = ReactionReader.ItemsOf(returned) ?? [returned];
        if (items.Count == 0)
        {
            return null;
        }
        var effects = items.Select(item => ReactionEffects.Read(item, model, handler)).ToList();
        if (effects.Exists(effect => effect is null || !EventReader.IsEvent(effect.Type) || (routesAppends && effect.Destination is null)))
        {
            return null;
        }
        var known = effects.OfType<ReactionEffect>().ToList();
        if (constraints?.UnprovenBatch([.. known.Select(effect => effect.Type)]) is not null)
        {
            return null;
        }
        var productions = known.ConvertAll(effect => Production(effect, model, handler, refusal, constraint));

        return productions.Exists(production => production is null) ? null : productions.OfType<ProducesModel>().ToList();
    }
}
