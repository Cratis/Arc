// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

/// <summary>
/// An enum declared in its own file, next to <see cref="Status"/> in the same namespace. Enums have
/// no methods and therefore no PDB sequence-point information to resolve a source file from.
/// </summary>
public enum StatusKind
{
    /// <summary>The status is a draft.</summary>
    Draft = 0,

    /// <summary>The status is active.</summary>
    Active = 1
}
