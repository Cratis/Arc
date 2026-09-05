// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Aggregates;

/// <summary>
/// Represents the expression a parameter was given at the call site it was reached through.
/// </summary>
/// <param name="Expression">The expression the caller handed over.</param>
/// <param name="SemanticModel">The semantic model of the tree the expression lives in.</param>
/// <remarks>
/// The expression belongs to the caller's tree, which is not necessarily the tree the parameter is used in, so the
/// model that can make sense of it travels with it.
/// </remarks>
public record BoundArgument(ExpressionSyntax Expression, SemanticModel SemanticModel);
