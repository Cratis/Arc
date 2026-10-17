// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Represents a rejecting guard a body opens with.
/// </summary>
/// <param name="Statement">The guard statement.</param>
/// <param name="Message">The literal rejection message, or null when the message is computed.</param>
/// <param name="Model">The semantic model of the body.</param>
internal sealed record ReadGuard(IfStatementSyntax Statement, string? Message, SemanticModel Model);
