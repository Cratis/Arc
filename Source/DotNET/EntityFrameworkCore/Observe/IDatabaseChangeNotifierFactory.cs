// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Defines a factory for creating <see cref="IDatabaseChangeNotifier"/> instances.
/// </summary>
public interface IDatabaseChangeNotifierFactory
{
    /// <summary>
    /// Creates a <see cref="IDatabaseChangeNotifier"/> for the specified DbContext.
    /// </summary>
    /// <param name="dbContext">The DbContext to create a notifier for.</param>
    /// <returns>A database change notifier appropriate for the database type.</returns>
    IDatabaseChangeNotifier Create(DbContext dbContext);
}
