// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// One handler or behavior body that is about to be read for the events it constructs.
/// </summary>
/// <param name="Syntax">The body to read.</param>
/// <param name="Model">The model the body is read through.</param>
/// <param name="Scope">The scope the body's productions are resolved in.</param>
/// <param name="Behavior">The behavior the body belongs to, when the handler reached it through an aggregate root.</param>
/// <param name="UsesCommandContext">Whether the handler or aggregate uses the command's event source context.</param>
/// <param name="Refusals">The effects of an unsupported returned tuple that must not be stated.</param>
internal sealed record ProducesBodyRead(
    SyntaxNode Syntax,
    SemanticModel Model,
    ProducesScope Scope,
    AggregateRootInvocation? Behavior,
    bool UsesCommandContext,
    ReturnedTupleRefusals? Refusals);
