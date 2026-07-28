// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a reference to a type - a Screenplay primitive, a concept or a declared model.
/// </summary>
/// <param name="Name">The name of the type being referenced.</param>
/// <param name="IsCollection">Whether the reference is to a collection of the type.</param>
/// <param name="IsOptional">Whether the reference is optional.</param>
public record TypeReferenceModel(string Name, bool IsCollection, bool IsOptional);
