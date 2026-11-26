// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents a slice of an application.
/// </summary>
/// <param name="Name">The name of the slice.</param>
/// <param name="Path">The path that identifies the slice, made of the module and features it sits in, separated by '/'.</param>
/// <param name="Parts">The commands and read models of the slice.</param>
public record StructureSlice(string Name, string Path, IReadOnlyList<StructurePart> Parts);
