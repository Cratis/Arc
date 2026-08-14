// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

/// <summary>
/// A type with a real method, declared in its own file alongside <see cref="StatusKind"/>.
/// </summary>
public class Status
{
    /// <summary>
    /// Describes the status.
    /// </summary>
    /// <returns>A description of the status.</returns>
    public string Describe() => nameof(Status);
}
