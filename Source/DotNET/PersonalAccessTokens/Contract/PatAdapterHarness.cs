// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>
/// Adapter-owned test setup. Each instance must provide an isolated empty store and enabled synthetic policy fixtures.
/// Exercise real persistence and external recovery evidence, not replacement test doubles.
/// </summary>
public sealed class PatAdapterHarness
{
    /// <summary>Gets the factory for a fresh isolated installation. An absent factory fails rather than skips.</summary>
    public Func<PatAdapterInstance>? CreateInstance { get; init; }
}
