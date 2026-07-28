// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a single property of a produced event and where its value comes from.
/// </summary>
/// <param name="Property">The name of the property being mapped onto, in its original casing.</param>
/// <param name="Source">Where the value comes from.</param>
public record PropertyMappingModel(string Property, MappingSourceModel Source);
