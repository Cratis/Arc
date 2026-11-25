// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents a command or read model of a slice.
/// </summary>
/// <param name="Name">The short name.</param>
/// <param name="FullName">The full name of the type, which is how availability names a part.</param>
/// <param name="Kind">Either <c>Command</c> or <c>ReadModel</c>.</param>
public record StructurePart(string Name, string FullName, string Kind);
