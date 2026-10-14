// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Recognizes Arc's invocation-local refusal branches without claiming declarative admission.
/// </summary>
static class RefusalInvocations
{
    /// <summary>
    /// Determines whether an invocation adds an Arc refusal branch, including acknowledgment.
    /// </summary>
    /// <param name="invocation">The invocation to inspect.</param>
    /// <param name="model">The semantic model resolving the Arc symbols.</param>
    /// <returns>Whether this calls one of the actual branch methods.</returns>
    internal static bool IsBranch(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        var invocationType = model.Compilation.GetTypeByMetadataName("Cratis.Arc.Chronicle.Reactors.CommandInvocation");

        return invocationType?.GetMembers().OfType<IMethodSymbol>()
            .Any(candidate => (string.Equals(candidate.Name, "OnRefused", StringComparison.Ordinal) ||
                string.Equals(candidate.Name, "Acknowledge", StringComparison.Ordinal)) &&
                SymbolEqualityComparer.Default.Equals(candidate, method.OriginalDefinition)) ?? false;
    }
}
