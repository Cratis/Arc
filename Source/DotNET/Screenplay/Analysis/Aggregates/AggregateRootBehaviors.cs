// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Aggregates;

/// <summary>
/// Finds the behaviors of an aggregate root that a command handler hands its work to.
/// </summary>
/// <remarks>
/// A command that governs its change through an aggregate root constructs no event itself - the aggregate root does,
/// from inside the behavior the handler called. Reading that behavior's body is what turns the command from one that
/// appears to produce nothing into one that states exactly what it produces, so the two ways of writing an Arc
/// command describe the same thing in the document.
/// <para>
/// Only the behaviors the handler calls directly are followed. A behavior calling another is code deciding for
/// itself, and following it would state a production the handler never asked for.
/// </para>
/// </remarks>
public static class AggregateRootBehaviors
{
    /// <summary>
    /// Finds the behaviors a handler body reaches.
    /// </summary>
    /// <param name="body">The body of the handler.</param>
    /// <param name="semanticModel">The model the body is read through.</param>
    /// <returns>The behaviors, in the order the handler calls them.</returns>
    public static IEnumerable<AggregateRootInvocation> ReachedFrom(SyntaxNode body, SemanticModel semanticModel)
    {
        var reached = new List<AggregateRootInvocation>();

        foreach (var call in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(call).Symbol is not IMethodSymbol { MethodKind: MethodKind.Ordinary } method ||
                !AggregateRoots.IsDeclaredByApplication(method.ContainingType))
            {
                continue;
            }

            reached.AddRange(HandlerBodies.Of(method).Select(declaration =>
                new AggregateRootInvocation(
                    method.ContainingType,
                    declaration,
                    ParameterBindings.For(method, call, semanticModel))));
        }

        return reached;
    }
}
