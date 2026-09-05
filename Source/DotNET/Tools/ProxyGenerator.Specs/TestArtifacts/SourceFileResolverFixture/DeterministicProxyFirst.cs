// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // Multiple types intentionally share a source file to exercise combined output.

namespace Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

/// <summary>
/// The first type used to verify deterministic combined proxy output.
/// </summary>
public class DeterministicProxyFirst
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// The second type used to verify deterministic combined proxy output.
/// </summary>
public class DeterministicProxySecond
{
    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    public int Count { get; set; }
}

#pragma warning restore SA1402 // Multiple types intentionally share a source file to exercise combined output.
