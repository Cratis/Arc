// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a manager for <see cref="QueryContext"/>.
/// </summary>
public interface IQueryContextManager
{
    /// <summary>
    /// Gets the current <see cref="QueryContext"/>.
    /// </summary>
    QueryContext Current { get; }

    /// <summary>
    /// Sets the <see cref="QueryContext"/>.
    /// </summary>
    /// <param name="context"><see cref="QueryContext"/> to set.</param>
    void Set(QueryContext context);
}
