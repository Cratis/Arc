// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Represents the body a production was read from, and everything needed to follow a value in it back to the command.
/// </summary>
/// <param name="SemanticModel">The semantic model of the tree the body lives in.</param>
/// <param name="Command">The type whose properties count as the command's own input.</param>
/// <param name="Bindings">What the handler gave the body's parameters, when the body is one it called.</param>
/// <param name="AggregateRoot">The aggregate root declaring the body, when the body is a behavior of one.</param>
/// <remarks>
/// A handler's own body and a behavior it hands its work to are read the same way, and the only difference between
/// them is what stands in for the names they use. Carrying that difference in one place is what lets a condition be
/// resolved back to command input wherever it was written, rather than only in the handler.
/// </remarks>
public record ProducesScope(
    SemanticModel SemanticModel,
    ITypeSymbol Command,
    ParameterBindings? Bindings,
    INamedTypeSymbol? AggregateRoot);
