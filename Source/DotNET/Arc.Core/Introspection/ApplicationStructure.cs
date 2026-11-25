// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents the structure of an application: its modules, features, sub-features and slices.
/// </summary>
/// <param name="Modules">The modules of the application. Parts that belong to no module are in a module with an empty name.</param>
public record ApplicationStructure(IReadOnlyList<StructureModule> Modules);
