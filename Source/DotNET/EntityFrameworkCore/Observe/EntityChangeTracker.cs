// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Represents an implementation of <see cref="IEntityChangeTracker"/>.
/// </summary>
public class EntityChangeTracker : IEntityChangeTracker
{
    readonly ConcurrentDictionary<Type, ConcurrentBag<Action>> _callbacks = new();

    /// <inheritdoc/>
    public IDisposable RegisterCallback<TEntity>(Action callback)
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        var callbacks = _callbacks.GetOrAdd(entityType, _ => []);
        callbacks.Add(callback);

        return new CallbackDisposer(entityType, callback, _callbacks);
    }

    /// <inheritdoc/>
    public void NotifyChange(Type entityType)
    {
        if (!_callbacks.TryGetValue(entityType, out var callbacks))
        {
            return;
        }

        foreach (var callback in callbacks)
        {
            callback();
        }
    }

    sealed class CallbackDisposer(Type entityType, Action callback, ConcurrentDictionary<Type, ConcurrentBag<Action>> callbacks) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (!callbacks.TryGetValue(entityType, out var bag))
            {
                return;
            }

            // ConcurrentBag doesn't support removal, so we need to recreate without the callback
            var newBag = new ConcurrentBag<Action>(bag.Where(c => c != callback));
            callbacks.TryUpdate(entityType, newBag, bag);
        }
    }
}
