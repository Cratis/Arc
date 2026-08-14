// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Represents the construction a step of a specification states, with the model it is read through.
/// </summary>
/// <param name="Creation">The construction as it is written.</param>
/// <param name="SemanticModel">The semantic model of the tree it lives in.</param>
/// <remarks>
/// The model travels with the construction because the two need not share a tree. A specification holding what it
/// starts from in a member assigns it in one file and states it in another as soon as the member is inherited, and a
/// model only answers about the tree it was asked for.
/// </remarks>
public record HeldConstruction(BaseObjectCreationExpressionSyntax Creation, SemanticModel SemanticModel);
