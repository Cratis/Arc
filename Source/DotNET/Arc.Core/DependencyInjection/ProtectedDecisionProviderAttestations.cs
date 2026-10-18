// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Attests native Microsoft DI providers without injectable bearer certificates or provider wrappers.
/// </summary>
internal static class ProtectedDecisionProviderAttestations
{
    static readonly ConditionalWeakTable<IServiceProvider, ProtectedDecisionScopeAttestation> _providers = new();
    static readonly ConditionalWeakTable<IServiceScopeFactory, ProtectedDecisionRootAttestation> _roots = new();
    static readonly ConditionalWeakTable<Type, object> _scopeTypes = new();

    /// <summary>
    /// Records the root and its built-in scope factory before returning it to application code.
    /// </summary>
    /// <param name="provider">The adapter-built root.</param>
    /// <param name="registrations">The exact ordered registrations used to build it.</param>
    internal static void TrackRoot(ServiceProvider provider, IReadOnlyList<ServiceDescriptor> registrations)
    {
        var canonical = provider.GetRequiredService<IServiceProvider>();
        var lifetime = provider.GetRequiredService<ProtectedDecisionProviderLifetime>();
        var root = new ProtectedDecisionRootAttestation(provider, canonical, canonical.GetType(), registrations, lifetime);
        _roots.Add(provider.GetRequiredService<IServiceScopeFactory>(), root);
        _providers.Add(provider, new(root, provider, lifetime));
        _providers.Add(canonical, new(root, canonical, lifetime));
        if (root.ScopeProviderType.IsSealed)
        {
            _scopeTypes.GetValue(root.ScopeProviderType, _ => new object());
        }
    }

    /// <summary>
    /// Tries to attest a live native child scope, refusing roots, clones and forwarding providers.
    /// </summary>
    /// <param name="executingProvider">The provider actually supplied for execution.</param>
    /// <param name="attestation">The scope attestation when eligible.</param>
    /// <returns>Whether the supplied provider is an eligible native scope.</returns>
    internal static bool TryGetAttestation(IServiceProvider executingProvider, [NotNullWhen(true)] out ProtectedDecisionScopeAttestation? attestation)
    {
        attestation = null;
        if (_providers.TryGetValue(executingProvider, out var candidate) &&
            (ReferenceEquals(executingProvider, candidate.Root.Provider) || ReferenceEquals(executingProvider, candidate.Root.CanonicalProvider)))
        {
            return false;
        }
        if (!_scopeTypes.TryGetValue(executingProvider.GetType(), out _))
        {
            return false;
        }
        try
        {
            // Recheck even recorded native scopes: a throwing disposable can prevent sentinel disposal.
            // Only an exact, sealed native provider reaches this resolution. Never trust a forwarded probe.
            executingProvider.GetService<ProtectedDecisionScopeProbe>();
        }
        catch (ObjectDisposedException)
        {
            // Microsoft DI refuses resolution after scope or root disposal, including failed disposal.
            return false;
        }
        if (!_providers.TryGetValue(executingProvider, out candidate) || !candidate.IsAlive)
        {
            return false;
        }
        attestation = candidate;
        return true;
    }

    /// <summary>
    /// Attaches only the native scope supplied by a known root's built-in DI scope factory.
    /// </summary>
    /// <param name="provider">The provider passed to the scoped activation factory.</param>
    /// <returns>The disposable scope probe, which alone never confers eligibility.</returns>
    internal static ProtectedDecisionScopeProbe CreateProbe(IServiceProvider provider)
    {
        var probe = new ProtectedDecisionScopeProbe();
        var factory = provider.GetRequiredService<IServiceScopeFactory>();
        if (_roots.TryGetValue(factory, out var root) &&
            !root.Lifetime.IsDisposed &&
            provider.GetType() == root.ScopeProviderType &&
            root.ScopeProviderType.IsSealed &&
            ReferenceEquals(provider, provider.GetRequiredService<IServiceProvider>()) &&
            !ReferenceEquals(provider, root.CanonicalProvider))
        {
            _providers.GetValue(provider, scope => new(root, scope, probe.Lifetime));
        }
        return probe;
    }
}
