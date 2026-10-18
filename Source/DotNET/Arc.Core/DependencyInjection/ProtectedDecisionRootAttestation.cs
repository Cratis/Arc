// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Binds a final registration snapshot to the exact native root that was built.
/// </summary>
/// <param name="Provider">The returned root provider.</param>
/// <param name="CanonicalProvider">The native provider used by root activation factories.</param>
/// <param name="ScopeProviderType">The native scope provider runtime type.</param>
/// <param name="Registrations">The immutable ordered descriptor snapshot, including probes.</param>
/// <param name="Lifetime">The eagerly resolved root lifetime sentinel.</param>
internal sealed record ProtectedDecisionRootAttestation(
    IServiceProvider Provider,
    IServiceProvider CanonicalProvider,
    Type ScopeProviderType,
    IReadOnlyList<ServiceDescriptor> Registrations,
    ProtectedDecisionProviderLifetime Lifetime);
