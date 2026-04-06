// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a set of changes describing which items were added, replaced, or removed in an
/// observable query result since the previous update.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ChangeSet"/> is optionally attached to a <see cref="QueryResult"/> when the
/// client has requested delta-mode transfer (via the <c>observableQueryTransferMode</c>
/// configuration).  In delta mode the frontend applies the change set to its local copy of the
/// collection instead of replacing the entire dataset, reducing the amount of data transferred
/// over the wire.
/// </para>
/// <para>
/// When a <see cref="ChangeSet"/> is <see langword="null"/> on a <see cref="QueryResult"/>, the client
/// must treat the <see cref="QueryResult.Data"/> field as the full current snapshot (full mode).
/// </para>
/// </remarks>
public class ChangeSet
{
    /// <summary>
    /// Gets or sets the items that were added since the previous update.
    /// </summary>
    public IEnumerable<object> Added { get; set; } = [];

    /// <summary>
    /// Gets or sets the items that were replaced (same identity, updated content) since the previous update.
    /// </summary>
    public IEnumerable<object> Replaced { get; set; } = [];

    /// <summary>
    /// Gets or sets the items that were removed since the previous update.
    /// </summary>
    public IEnumerable<object> Removed { get; set; } = [];
}
