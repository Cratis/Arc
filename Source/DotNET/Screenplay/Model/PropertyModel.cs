// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a property of a command, an event or a query parameter.
/// </summary>
/// <param name="Name">The name of the property, in its original casing - emission camel cases it.</param>
/// <param name="Type">The type of the property.</param>
public record PropertyModel(string Name, TypeReferenceModel Type);
