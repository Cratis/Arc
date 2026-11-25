// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// Provides a value the host supplies by name. Opaque to model tooling; absent when the host does not supply it.
/// </summary>
/// <param name="host">The host values.</param>
/// <param name="hostName">The name the host knows the value by.</param>
public class HostContextValueProvider(IHostContextValues host, string hostName) : IContextValueProvider
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> Dependencies => [];

    /// <inheritdoc/>
    public ValueTask<ContextValueOutcome> Provide(IContextValueResolver context, CancellationToken cancellationToken) =>
        host.Get(hostName, cancellationToken);
}
