// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents the read model a slice builds.
/// </summary>
/// <param name="Id">The identity of the read model.</param>
/// <param name="Name">The name of the read model.</param>
/// <param name="Schema">The JSON Schema of the state the read model holds.</param>
/// <param name="Materializes">Whether the read model is materialized rather than computed per read.</param>
public record ReadModelItem(Guid Id, string Name, JsonObject Schema, bool Materializes);
