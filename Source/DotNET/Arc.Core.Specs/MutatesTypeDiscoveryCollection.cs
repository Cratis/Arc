// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Collection definition for specs that add to the process-wide <see cref="GeneratedTypeDiscoveryRegistry"/> or
/// assert on the type universe <c>AddCratisArcCore</c> leaves behind in the equally process-wide
/// <c>Internals.Types</c>, or that read the equally process-wide <c>Internals.DerivedTypes</c> indirectly through
/// <c>new ArcOptions()</c>.
/// </summary>
/// <remarks>
/// Every spec that configures Arc writes <c>Internals.Types</c> and <c>Internals.DerivedTypes</c>, so a spec reading it back races
/// with any test class running in parallel - including on assertions that hold for the value its own call wrote.
/// </remarks>
[CollectionDefinition("MutatesTypeDiscovery", DisableParallelization = true)]
public class MutatesTypeDiscoveryCollection;
