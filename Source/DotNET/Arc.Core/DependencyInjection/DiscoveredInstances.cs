// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Resolves discovered implementations of an extension point from the service provider that owns the work being done.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IInstancesOf{T}"/> resolves each implementation lazily, but from the <see cref="IServiceProvider"/> that
/// constructed it. A singleton holding one therefore resolves every implementation from the root provider, and an
/// implementation that depends on a scoped service cannot be created there — the container rejects it outright once
/// scope validation is on, which is what the host enables in Development.
/// </para>
/// <para>
/// The systems that hold these collections are legitimately singletons: they are held in turn by the command and query
/// pipelines, which are singletons themselves because they create the scope a command runs in. So rather than moving
/// the collections into the scope, they resolve their implementations from the scope at the point of use — the same
/// provider Arc already threads through <c>CommandContext</c> and <c>QueryContext</c> for exactly this purpose.
/// </para>
/// </remarks>
public static class DiscoveredInstances
{
    /// <summary>
    /// Gets the discovered implementations of <typeparamref name="T"/>, resolved from the given <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">Type of the extension point to get implementations of.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the work being done, if there is one.</param>
    /// <param name="fallback">The <see cref="IInstancesOf{T}"/> to use when there is no service provider to resolve from.</param>
    /// <returns>The discovered implementations of <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// Falls back to <paramref name="fallback"/> when there is no service provider — a command executed without one
    /// has no scope for an implementation to be resolved from, and the injected collection is the only set available.
    /// </remarks>
    public static IEnumerable<T> ResolvedFrom<T>(IServiceProvider? serviceProvider, IInstancesOf<T> fallback)
        where T : class =>
        serviceProvider?.GetService<IInstancesOf<T>>() ?? fallback;
}
