// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents one explicitly typed and mapped part of a composite stream id.
/// </summary>
/// <param name="Name">The part name.</param>
/// <param name="Type">The required scalar type of the part.</param>
/// <param name="Value">The direct command input or literal supplying the part.</param>
public record CommandStreamIdPartModel(string Name, TypeReferenceModel Type, MappingSourceModel Value);
