// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Defines a service that tracks and notifies about entity changes.
/// </summary>
public interface IEntityChangeTracker
{
    /// <summary>
    /// Registers a callback to be invoked when an entity of the specified type changes.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to track.</typeparam>
    /// <param name="callback">The callback to invoke when changes occur.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to unregister the callback.</returns>
    IDisposable RegisterCallback<TEntity>(Action callback)
        where TEntity : class;

    /// <summary>
    /// Notifies all registered callbacks for the specified entity type that a change has occurred.
    /// </summary>
    /// <param name="entityType">The type of entity that has changed.</param>
    void NotifyChange(Type entityType);
}
