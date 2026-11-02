// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Owns the service scope that belongs to one observable-query hub subscription.
/// </summary>
/// <remarks>
/// Invariant I-3600, one subscription, one scope: a subscription context created by the hub resolves only through a
/// service scope that belongs to that subscription alone, or through a scope the pipeline selected for that same
/// subscription. It never resolves through the root provider or another subscription's scope.
/// The scope is created together with the context. It is disposed exactly once: by the creator when the subscription
/// never becomes a live stream, or by the stream's <see cref="StreamingQuerySubscription"/> once it has drained.
/// Ownership moves with <see cref="TryTransferTo"/>; an interlocked exchange makes transfer and disposal mutually exclusive.
/// After disposal a late read through the provider fails closed with <see cref="ObjectDisposedException"/>.
/// </remarks>
internal sealed class ObservableQuerySubscriptionServiceScope : IDisposable
{
    IServiceScope? _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableQuerySubscriptionServiceScope"/> class.
    /// </summary>
    /// <param name="root">The provider to create the subscription scope from.</param>
    public ObservableQuerySubscriptionServiceScope(IServiceProvider root)
        : this(root.CreateScope())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableQuerySubscriptionServiceScope"/> class.
    /// </summary>
    /// <param name="scope">The scope to own.</param>
    internal ObservableQuerySubscriptionServiceScope(IServiceScope scope)
    {
        _scope = scope;
        ServiceProvider = scope.ServiceProvider;
    }

    /// <summary>
    /// Gets the provider of the subscription's own scope.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Hands ownership of the scope to a live subscription, which disposes it once it has drained.
    /// </summary>
    /// <param name="lifetime">The subscription lifetime that takes ownership.</param>
    /// <returns>True when ownership was transferred; false when the scope was already transferred or disposed.</returns>
    public bool TryTransferTo(StreamingQuerySubscription lifetime)
    {
        var scope = Interlocked.Exchange(ref _scope, null);
        if (scope is null)
        {
            return false;
        }

        lifetime.AddResource(scope);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose() => Interlocked.Exchange(ref _scope, null)?.Dispose();
}
