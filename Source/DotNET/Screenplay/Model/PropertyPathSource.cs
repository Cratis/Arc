// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a value taken from a property of the command.
/// </summary>
/// <param name="Path">The dotted path to the property, in its original casing.</param>
public record PropertyPathSource(string Path) : MappingSourceModel;
