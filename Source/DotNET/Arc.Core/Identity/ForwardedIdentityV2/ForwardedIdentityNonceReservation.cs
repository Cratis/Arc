// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>The atomic replay reservation outcome.</summary>
internal enum ForwardedIdentityNonceReservation
{
    /// <summary>A new reservation was recorded.</summary>
    Reserved = 0,

    /// <summary>The nonce is already live.</summary>
    Replayed = 1,

    /// <summary>The store cannot reserve safely.</summary>
    Unavailable = 2
}
