// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>The atomic replay reservation outcome.</summary>
public enum ForwardedIdentityNonceReservation
{
    /// <summary>A new reservation was recorded.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1700:Do not name enum values 'Reserved'", Justification = "Reserved denotes a successfully recorded nonce reservation, not a reserved placeholder.")]
    Reserved = 0,

    /// <summary>The nonce is already live.</summary>
    Replayed = 1,

    /// <summary>The store cannot reserve safely.</summary>
    Unavailable = 2
}
