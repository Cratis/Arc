// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Describes how much is known about where an operation's tenant came from.
/// </summary>
internal enum TenantProvenance
{
    /// <summary>
    /// No tenant was resolved.
    /// </summary>
    NotSet = 0,

    /// <summary>
    /// The tenant was explicitly selected by an <c>ITenantScope</c>.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// The tenant was cached, authorized or resolved without a membership check.
    /// </summary>
    Unverified = 2,

    /// <summary>
    /// The tenant was resolved and verified by the membership checks.
    /// </summary>
    Verified = 3
}
