// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Represents a packed nupkg and its dependency metadata.
/// </summary>
/// <param name="Path">The nupkg path.</param>
/// <param name="Entries">The archive entries.</param>
/// <param name="Dependencies">The nuspec dependencies, including their group and asset metadata.</param>
public sealed record PackageArchive(
    string Path,
    IReadOnlyCollection<string> Entries,
    IReadOnlyCollection<PackageDependency> Dependencies);
