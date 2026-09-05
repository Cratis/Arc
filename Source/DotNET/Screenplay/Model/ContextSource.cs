// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a value taken from the ambient execution context.
/// </summary>
/// <param name="Path">The dotted path within the context, for example <c>occurred</c> or <c>identity.id</c>.</param>
public record ContextSource(string Path) : MappingSourceModel;
