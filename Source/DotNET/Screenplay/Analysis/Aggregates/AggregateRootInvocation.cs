// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Aggregates;

/// <summary>
/// Represents one behavior of an aggregate root, reached from the handler of a command.
/// </summary>
/// <param name="AggregateRoot">The aggregate root declaring the behavior.</param>
/// <param name="Body">The body of the behavior.</param>
/// <param name="Bindings">What the handler gave the behavior's parameters.</param>
public record AggregateRootInvocation(
    INamedTypeSymbol AggregateRoot,
    SyntaxNode Body,
    ParameterBindings Bindings);
