// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Represents what a Provide method rejects and the read it passes on.
/// </summary>
/// <param name="Requirements">The requirements its guards state.</param>
/// <param name="Provided">The expression it passes on.</param>
/// <param name="Model">The semantic model of the method.</param>
internal sealed record ProvisionRead(IReadOnlyList<CommandRequirementModel> Requirements, ExpressionSyntax Provided, SemanticModel Model);
