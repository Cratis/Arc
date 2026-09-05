// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Defines the shared package-graph integration fixture.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PackageGraphCollection : ICollectionFixture<PackageGraphFixture>
{
    /// <summary>
    /// The collection name.
    /// </summary>
    public const string Name = "Analyzer package graph";
}
