// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Represents an implementation of <see cref="IEntityChangeTracker"/>.
/// </summary>
/// <remarks>
/// Uses table names instead of CLR types to avoid issues with EF Core proxy classes
/// and derived types. Table names are stable identifiers that work regardless of
/// the runtime type of entities.
/// </remarks>
public class EntityChangeTracker : IEntityChangeTracker
{
    readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Action>> _callbacks = new();

    /// <inheritdoc/>
    public IDisposable RegisterCallback(string tableName, Action callback)
    {
        var subscriptionId = Guid.NewGuid();
        var callbacks = _callbacks.GetOrAdd(tableName, _ => new ConcurrentDictionary<Guid, Action>());
        callbacks[subscriptionId] = callback;

        return new CallbackDisposer(tableName, subscriptionId, _callbacks);
    }

    /// <inheritdoc/>
    public void NotifyChange(string tableName)
    {
        if (!_callbacks.TryGetValue(tableName, out var callbacks))
        {
            return;
        }

        foreach (var callback in callbacks.Values)
        {
            callback();
        }
    }

    sealed class CallbackDisposer(string tableName, Guid subscriptionId, ConcurrentDictionary<string, ConcurrentDictionary<Guid, Action>> callbacks) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (callbacks.TryGetValue(tableName, out var tableCallbacks))
            {
                tableCallbacks.TryRemove(subscriptionId, out _);
            }
        }
    }
}
