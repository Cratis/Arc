// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Why a <see cref="StateAwareAuthorizationPolicy{TState}"/> denied access, for diagnostics.
/// </summary>
public enum StateDenialReason
{
    /// <summary>
    /// The command or query did not carry a key to look the state up by.
    /// </summary>
    NoKey = 0,

    /// <summary>
    /// No recorded state exists for the key. Absence denies; it never means "allowed".
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// The state could not be read, for example because the store was unavailable.
    /// </summary>
    ReadFailed = 2,

    /// <summary>
    /// The state was read and the decision rejected the caller.
    /// </summary>
    Rejected = 3
}
