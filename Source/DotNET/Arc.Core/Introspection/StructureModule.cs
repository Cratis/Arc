// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents a module of an application.
/// </summary>
/// <param name="Name">The name of the module; empty for what belongs to no module.</param>
/// <param name="Path">The path that identifies the module, the same as its name.</param>
/// <param name="Features">The features of the module.</param>
/// <param name="Slices">The slices that sit directly in the module.</param>
public record StructureModule(string Name, string Path, IReadOnlyList<StructureFeature> Features, IReadOnlyList<StructureSlice> Slices);
