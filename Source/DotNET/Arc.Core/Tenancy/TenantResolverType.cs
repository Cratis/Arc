// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents the different types of tenant ID resolvers.
/// </summary>
public enum TenantResolverType
{
    /// <summary>
    /// Resolve tenant ID from HTTP header.
    /// </summary>
    Header = 0,

    /// <summary>
    /// Resolve tenant ID from query string.
    /// </summary>
    Query = 1,

    /// <summary>
    /// Resolve tenant ID from claims in the authenticated user.
    /// </summary>
    Claim = 2,

    /// <summary>
    /// Use a fixed tenant ID for development purposes.
    /// </summary>
    Development = 3
}
