// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents what a connection is going to be used for.
/// </summary>
/// <remarks>
/// A platform can hand out different identities per purpose, for instance one database user that may only read and
/// write data and another that may also create and alter tables.
/// </remarks>
public enum ConnectionPurpose
{
    /// <summary>
    /// The connection is used for data access.
    /// </summary>
    Runtime = 0,

    /// <summary>
    /// The connection is used for changing the database structure — creating databases and schemas, running migrations
    /// and altering tables.
    /// </summary>
    Schema = 1
}
