// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents the command a slice handles.
/// </summary>
/// <param name="Id">The identity of the command.</param>
/// <param name="Name">The name of the command.</param>
/// <param name="Schema">The JSON Schema of what the command carries.</param>
/// <param name="StateSchema">The JSON Schema of the state the command decides against - an empty object schema, Screenplay states this as the read models it reads.</param>
/// <param name="LogicDescription">The description of what the command does.</param>
/// <param name="Rules">The validation rules declared per property.</param>
public record CommandItem(
    Guid Id,
    string Name,
    JsonObject Schema,
    JsonObject StateSchema,
    string LogicDescription,
    IReadOnlyList<CommandPropertyRules> Rules);
