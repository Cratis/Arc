// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Lets native DI scope activation attest the provider passed to its factory.
/// </summary>
internal sealed class ProtectedDecisionScopeProbe : IDisposable
{
    /// <summary>
    /// Gets the sentinel disposed with this scoped probe.
    /// </summary>
    internal ProtectedDecisionProviderLifetime Lifetime { get; } = new();

    /// <inheritdoc/>
    public void Dispose() => Lifetime.Dispose();
}
