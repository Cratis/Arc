// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents the structure of an application: its modules, features, sub-features and slices.
/// </summary>
/// <param name="Modules">The modules of the application. Parts that belong to no module are in a module with an empty name.</param>
public record ApplicationStructure(IReadOnlyList<StructureModule> Modules);

/// <summary>
/// Represents a module of an application.
/// </summary>
/// <param name="Name">The name of the module; empty for what belongs to no module.</param>
/// <param name="Path">The path that identifies the module, the same as its name.</param>
/// <param name="Features">The features of the module.</param>
/// <param name="Slices">The slices that sit directly in the module.</param>
public record StructureModule(string Name, string Path, IReadOnlyList<StructureFeature> Features, IReadOnlyList<StructureSlice> Slices);

/// <summary>
/// Represents a feature, or a sub-feature, of an application.
/// </summary>
/// <param name="Name">The name of the feature.</param>
/// <param name="Path">The path that identifies the feature, made of the module and the features it is nested in, separated by '/'.</param>
/// <param name="Features">The sub-features of the feature.</param>
/// <param name="Slices">The slices that sit directly in the feature.</param>
public record StructureFeature(string Name, string Path, IReadOnlyList<StructureFeature> Features, IReadOnlyList<StructureSlice> Slices);

/// <summary>
/// Represents a slice of an application.
/// </summary>
/// <param name="Name">The name of the slice.</param>
/// <param name="Path">The path that identifies the slice, made of the module and features it sits in, separated by '/'.</param>
/// <param name="Parts">The commands and read models of the slice.</param>
public record StructureSlice(string Name, string Path, IReadOnlyList<StructurePart> Parts);

/// <summary>
/// Represents a command or read model of a slice.
/// </summary>
/// <param name="Name">The short name.</param>
/// <param name="FullName">The full name of the type, which is how availability names a part.</param>
/// <param name="Kind">Either <c>Command</c> or <c>ReadModel</c>.</param>
public record StructurePart(string Name, string FullName, string Kind);
