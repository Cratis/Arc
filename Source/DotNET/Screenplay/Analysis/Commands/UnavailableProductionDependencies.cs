// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Retains unavailable control-flow and explicit-destination dependencies before syntax recovery loses them.
/// </summary>
internal static class UnavailableProductionDependencies
{
    /// <summary>
    /// Finds unavailable enclosing decisions and preceding exit guards.
    /// </summary>
    /// <param name="creation">The event construction.</param>
    /// <param name="body">The enclosing handler body.</param>
    /// <param name="sources">The symbol-keyed generated inputs.</param>
    /// <param name="model">The semantic model resolving dependencies.</param>
    /// <param name="bindings">The arguments supplied to a reached aggregate.</param>
    /// <returns>The unavailable guard input, or null.</returns>
    internal static UnavailableGeneratedValueModel? Condition(SyntaxNode creation, SyntaxNode body, AuthoringSources? sources, SemanticModel model, ParameterBindings? bindings)
    {
        if (sources?.HasUnavailable != true)
        {
            return null;
        }

        var uses = body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Where(identifier => LocalValueOrigins.Reaches(identifier, creation, model)).Cast<SyntaxNode>().Prepend(creation);

        return uses.Select(use => ConditionAt(use, body, sources, model, bindings)).FirstOrDefault(dependency => dependency is not null);
    }

    /// <summary>
    /// Finds unavailable explicit wrapper or tuple destinations without retargeting them.
    /// </summary>
    /// <param name="creation">The event construction.</param>
    /// <param name="body">The enclosing handler body.</param>
    /// <param name="sources">The symbol-keyed generated inputs.</param>
    /// <param name="model">The semantic model resolving dependencies.</param>
    /// <returns>The unavailable destination input, or null.</returns>
    internal static UnavailableGeneratedValueModel? Destination(SyntaxNode creation, SyntaxNode body, AuthoringSources? sources, SemanticModel model)
    {
        if (sources?.HasUnavailable != true)
        {
            return null;
        }

        foreach (var wrapper in body.DescendantNodesAndSelf().OfType<BaseObjectCreationExpressionSyntax>().Where(wrapper =>
            model.GetTypeInfo(wrapper).Type.Is("Cratis.Chronicle.EventSequences.EventForEventSourceId")))
        {
            if (model.GetOperation(wrapper) is not IObjectCreationOperation operation ||
                operation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 1)?.Syntax is not ArgumentSyntax payload ||
                !LocalValueOrigins.Reaches(payload.Expression, creation, model))
            {
                continue;
            }
            var dependencies = operation.Arguments.Where(argument => argument.Parameter?.Ordinal != 1).Select(argument => argument.Syntax)
                .Concat(wrapper.Initializer is null ? [] : new SyntaxNode[] { wrapper.Initializer });
            if (dependencies.Select(expression => sources.UnavailableIn(expression, model)).FirstOrDefault(value => value is not null) is { } unavailable)
            {
                return unavailable;
            }
        }
        foreach (var ancestor in body.DescendantNodesAndSelf().OfType<TupleExpressionSyntax>().Where(tuple => tuple.Arguments.Any(argument =>
            model.GetTypeInfo(argument.Expression).Type is { } type && EventReader.IsEvent(type) && LocalValueOrigins.Reaches(argument.Expression, creation, model))))
        {
            var type = model.GetTypeInfo(ancestor);
            var identity = ReturnedProductionShape.IdentityOf(ancestor, model);
            if (!EventResponseTuples.UsesCommandContext(type.ConvertedType ?? type.Type))
            {
                foreach (var argument in ancestor.Arguments.Where(argument => argument.Expression == identity || ReturnedProductionShape.IsIdentity(model.GetTypeInfo(argument.Expression).Type)))
                {
                    if (sources.UnavailableIn(argument.Expression, model) is { } dependency)
                    {
                        return dependency;
                    }
                }
            }
        }

        return null;
    }

    static UnavailableGeneratedValueModel? ConditionAt(SyntaxNode use, SyntaxNode body, AuthoringSources sources, SemanticModel model, ParameterBindings? bindings)
    {
        for (var node = use; node is not null && node != body; node = node.Parent)
        {
            var guard = node.Parent switch
            {
                IfStatementSyntax statement => statement.Condition,
                ConditionalExpressionSyntax conditional => conditional.Condition,
                SwitchStatementSyntax statement => statement.Expression,
                SwitchExpressionSyntax expression => expression.GoverningExpression,
                SwitchExpressionArmSyntax arm => arm.WhenClause?.Condition,
                _ => null
            };
            if (guard is not null && sources.UnavailableIn(guard, model, bindings) is { } dependency)
            {
                return dependency;
            }
            if (node.Parent is BlockSyntax block)
            {
                foreach (var preceding in block.Statements.TakeWhile(statement => statement != node).OfType<IfStatementSyntax>()
                    .Where(statement => statement.Else is null && AlwaysExits(statement.Statement)))
                {
                    if (sources.UnavailableIn(preceding.Condition, model, bindings) is { } unavailable)
                    {
                        return unavailable;
                    }
                }
            }
        }

        return null;
    }

    static bool AlwaysExits(StatementSyntax statement) => statement switch
    {
        ReturnStatementSyntax or ThrowStatementSyntax => true,
        BlockSyntax block => block.Statements.Count > 0 && AlwaysExits(block.Statements[^1]),
        _ => false
    };
}
