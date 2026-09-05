// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Represents a built slice together with the namespace that determines where it is placed in the tree.
/// </summary>
/// <param name="Namespace">The namespace the slice lives in.</param>
/// <param name="Slice">The built <see cref="SliceSyntax"/>.</param>
public record PlacedSlice(string Namespace, SliceSyntax Slice);
