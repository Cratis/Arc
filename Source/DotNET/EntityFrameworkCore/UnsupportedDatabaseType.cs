// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Exception that is thrown when a database type is not supported.
/// </summary>
public class UnsupportedDatabaseType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedDatabaseType"/> class.
    /// </summary>
    public UnsupportedDatabaseType()
        : base("The configured connection does not have a supported database type.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedDatabaseType"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string. This value is ignored.</param>
    [Obsolete("The connection value is no longer part of the message; use the parameterless constructor.")]
    public UnsupportedDatabaseType(string connectionString)
        : this()
    {
    }
}
