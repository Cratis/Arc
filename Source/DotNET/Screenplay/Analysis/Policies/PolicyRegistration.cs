// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Represents one named authorization policy the application registers.
/// </summary>
/// <param name="Name">The name the policy is registered under.</param>
/// <param name="Configure">The argument declaring what the policy requires.</param>
/// <param name="SemanticModel">The semantic model of the tree the registration lives in.</param>
/// <param name="Location">The path of the file the registration lives in.</param>
public record PolicyRegistration(
    string Name,
    ExpressionSyntax? Configure,
    SemanticModel SemanticModel,
    string Location);
