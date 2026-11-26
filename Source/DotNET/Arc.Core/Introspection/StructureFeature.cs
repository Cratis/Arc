// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents a feature, or a sub-feature, of an application.
/// </summary>
/// <param name="Name">The name of the feature.</param>
/// <param name="Path">The path that identifies the feature, made of the module and the features it is nested in, separated by '/'.</param>
/// <param name="Features">The sub-features of the feature.</param>
/// <param name="Slices">The slices that sit directly in the feature.</param>
public record StructureFeature(string Name, string Path, IReadOnlyList<StructureFeature> Features, IReadOnlyList<StructureSlice> Slices);
