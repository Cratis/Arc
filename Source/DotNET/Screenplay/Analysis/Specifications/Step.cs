// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Represents one call a specification makes, with everything needed to read what it says.
/// </summary>
/// <param name="Invocation">The call as it is written.</param>
/// <param name="Method">The method it resolves to.</param>
/// <param name="SemanticModel">The semantic model of the tree it lives in.</param>
/// <param name="Always">Whether the body it is written in always makes the call, exactly once.</param>
public record Step(
    InvocationExpressionSyntax Invocation,
    IMethodSymbol Method,
    SemanticModel SemanticModel,
    bool Always);
