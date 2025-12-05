// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Represents an implementation of <see cref="IEntityChangeTracker"/>.
/// </summary>
public class EntityChangeTracker : IEntityChangeTracker
{
    readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Action>> _callbacks = new();

    /// <inheritdoc/>
    public IDisposable RegisterCallback<TEntity>(Action callback)
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        var subscriptionId = Guid.NewGuid();
        var callbacks = _callbacks.GetOrAdd(entityType, _ => new ConcurrentDictionary<Guid, Action>());
        callbacks[subscriptionId] = callback;

        return new CallbackDisposer(entityType, subscriptionId, _callbacks);
    }

    /// <inheritdoc/>
    public void NotifyChange(Type entityType)
    {
        if (!_callbacks.TryGetValue(entityType, out var callbacks))
        {
            return;
        }

        foreach (var callback in callbacks.Values)
        {
            callback();
        }
    }

    sealed class CallbackDisposer(Type entityType, Guid subscriptionId, ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Action>> callbacks) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (callbacks.TryGetValue(entityType, out var typeCallbacks))
            {
                typeCallbacks.TryRemove(subscriptionId, out _);
            }
        }
    }
}
