// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves that an event is handed back to the command pipeline rather than routed explicitly or constructed elsewhere.
/// </summary>
public static class ProductionDestinations
{
    /// <summary>
    /// Determines whether the construction reaches the command context through a return or an aggregate's Apply.
    /// </summary>
    /// <param name="creation">The event construction.</param>
    /// <param name="body">The handler or aggregate behavior body.</param>
    /// <param name="model">The semantic model of that body.</param>
    /// <param name="aggregate">Whether the body belongs to an aggregate of the command context.</param>
    /// <param name="generatedIdentity">Whether analysis proved a directly returned generated tuple identity.</param>
    /// <returns>Whether command-context routing is established.</returns>
    public static bool ThroughCommandContext(BaseObjectCreationExpressionSyntax creation, SyntaxNode body, SemanticModel model, bool aggregate, bool generatedIdentity = false)
    {
        for (SyntaxNode? node = creation; node is not null; node = node.Parent)
        {
            if (node is BaseObjectCreationExpressionSyntax wrapper && wrapper != creation &&
                model.GetTypeInfo(wrapper).Type is INamedTypeSymbol type && !IsReturnWrapper(type))
            {
                return false;
            }

            if (node is TupleExpressionSyntax tuple && (!generatedIdentity || !tuple.Arguments.Any(argument => argument.Expression == creation) ||
                (tuple.Parent is not ReturnStatementSyntax && !ReferenceEquals(tuple, body))))
            {
                return false;
            }

            if (node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
            {
                return false;
            }

            if (node is InvocationExpressionSyntax invocation)
            {
                var method = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (aggregate && method?.Name == "Apply" && AggregateRoots.Is(method.ContainingType))
                {
                    return true;
                }

                if (method is null || !IsReturnCall(method))
                {
                    return false;
                }
            }

            if (node is ReturnStatementSyntax || (ReferenceEquals(node, body) && body is ExpressionSyntax))
            {
                return true;
            }

            if (ReferenceEquals(node, body))
            {
                break;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether every call reaching an aggregate behavior addresses the command's own identity.
    /// </summary>
    /// <param name="behavior">The reached behavior.</param>
    /// <param name="body">The calling handler's body.</param>
    /// <param name="model">The model of the calling handler.</param>
    /// <param name="command">The command type.</param>
    /// <param name="identifier">The command's known identifier, when available.</param>
    /// <returns>Whether the aggregate is supplied by command context or fetched with precisely that identifier.</returns>
    public static bool AggregateUsesCommandContext(AggregateRootInvocation behavior, SyntaxNode body, SemanticModel model, INamedTypeSymbol command, string? identifier)
    {
        var calls = body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            .Where(call => model.GetSymbolInfo(call).Symbol is IMethodSymbol method &&
                HandlerBodies.Of(method).Any(candidate => candidate.SyntaxTree == behavior.Body.SyntaxTree && candidate.Span == behavior.Body.Span))
            .ToList();

        return calls.Count > 0 && calls.TrueForAll(call => call.Expression is MemberAccessExpressionSyntax member &&
            IsOwnAggregate(member.Expression, body, model, command, identifier));
    }

    static bool IsOwnAggregate(ExpressionSyntax receiver, SyntaxNode body, SemanticModel model, INamedTypeSymbol command, string? identifier)
    {
        receiver = MappingSourceReader.Unwrap(receiver);
        if (model.GetSymbolInfo(receiver).Symbol is IParameterSymbol parameter && AggregateRoots.Is(parameter.Type))
        {
            return parameter.ContainingSymbol is IMethodSymbol handler && handler.Name == CommandReader.HandleMethod &&
                SymbolEqualityComparer.Default.Equals(handler.ContainingType, command);
        }

        if (model.GetSymbolInfo(receiver).Symbol is ILocalSymbol local)
        {
            if (body.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(assignment =>
                SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, local)))
            {
                return false;
            }

            receiver = local.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is VariableDeclaratorSyntax { Initializer.Value: { } value }
                ? MappingSourceReader.Unwrap(value)
                : receiver;
        }

        if (receiver is AwaitExpressionSyntax awaited)
        {
            receiver = MappingSourceReader.Unwrap(awaited.Expression);
        }

        return identifier is not null && receiver is InvocationExpressionSyntax invocation &&
            model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && method.Name == "Get" &&
            method.ContainingType.FullMetadataName() == "Cratis.Arc.Chronicle.Aggregates.IAggregateRootFactory" &&
            invocation.ArgumentList.Arguments is [var key, ..] &&
            MappingSourceReader.ReadPath(key.Expression, model, command) == identifier;
    }

    static bool IsReturnWrapper(INamedTypeSymbol type)
    {
        var name = type.FullMetadataName();

        return name == "Cratis.Monads.Result`2" || name == "System.Collections.Generic.List`1" || name == "System.Threading.Tasks.ValueTask`1";
    }

    static bool IsReturnCall(IMethodSymbol method)
    {
        var name = method.ContainingType.FullMetadataName();

        return method.Name == "FromResult" && (name == "System.Threading.Tasks.Task" || name == "System.Threading.Tasks.ValueTask");
    }
}
