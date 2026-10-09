// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents what a specification does - the command it runs, or the action it takes instead.
/// </summary>
/// <param name="Id">The identity of the action.</param>
/// <param name="CommandId">The identity of the slice's command when the action runs it, otherwise null.</param>
/// <param name="Name">The name of the command, or the kind of action followed by what it acts on - <c>append AuthorRegistered</c>, <c>clock 2026-10-05T08:00:00Z</c>.</param>
/// <param name="Values">The values the action is given.</param>
public record SpecificationAction(Guid Id, Guid? CommandId, string Name, JsonObject Values);
