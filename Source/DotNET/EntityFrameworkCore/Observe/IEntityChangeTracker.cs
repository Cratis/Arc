// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Defines a service that tracks and notifies about entity changes.
/// </summary>
public interface IEntityChangeTracker
{
    /// <summary>
    /// Registers a callback to be invoked when entities in the specified table change.
    /// </summary>
    /// <param name="tableName">The database table name to track.</param>
    /// <param name="callback">The callback to invoke when changes occur.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to unregister the callback.</returns>
    IDisposable RegisterCallback(string tableName, Action callback);

    /// <summary>
    /// Notifies all registered callbacks for the specified table that a change has occurred.
    /// </summary>
    /// <param name="tableName">The name of the table that has changed.</param>
    void NotifyChange(string tableName);
}
