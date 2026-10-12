// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Pairs a setup call with the semantic model that owns its source tree.
/// </summary>
/// <param name="Syntax">The source invocation.</param>
/// <param name="Model">The owning semantic model.</param>
internal sealed record SpecificationCallerCall(InvocationExpressionSyntax Syntax, SemanticModel Model)
{
    /// <summary>
    /// Gets the resolved method.
    /// </summary>
    internal IMethodSymbol? Method => Model.GetSymbolInfo(Syntax).Symbol as IMethodSymbol;

    /// <summary>
    /// Gets whether the call is written in Establish.
    /// </summary>
    internal bool InEstablish => Syntax.FirstAncestorOrSelf<MethodDeclarationSyntax>()?.Identifier.ValueText == SpecificationMembers.EstablishMethod;

    /// <summary>
    /// Gets whether the enclosing member always reaches the call once.
    /// </summary>
    internal bool Unconditional => Syntax.FirstAncestorOrSelf<MemberDeclarationSyntax>() is { } member && StepsTaken.AlwaysCompletesThrough(Syntax, member);

    /// <summary>
    /// Gets the symbol receiving the invocation.
    /// </summary>
    internal ISymbol? Receiver => Syntax.Expression is MemberAccessExpressionSyntax member ? Model.GetSymbolInfo(member.Expression).Symbol : null;
}
