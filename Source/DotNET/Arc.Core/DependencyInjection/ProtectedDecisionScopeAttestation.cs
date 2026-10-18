// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Associates an exact native provider with its adapter-built root and lifetime.
/// </summary>
/// <param name="Root">The owning root attestation.</param>
/// <param name="Provider">The exact attested provider.</param>
/// <param name="Lifetime">The provider lifetime sentinel.</param>
internal sealed record ProtectedDecisionScopeAttestation(
    ProtectedDecisionRootAttestation Root,
    IServiceProvider Provider,
    ProtectedDecisionProviderLifetime Lifetime)
{
    /// <summary>
    /// Gets whether neither sentinel has observed disposal.
    /// </summary>
    internal bool IsAlive => !Root.Lifetime.IsDisposed && !Lifetime.IsDisposed;
}
