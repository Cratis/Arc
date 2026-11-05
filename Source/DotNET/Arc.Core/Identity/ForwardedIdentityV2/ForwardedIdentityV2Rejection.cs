// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Fixed, safe-to-log verification refusal reasons.</summary>
internal enum ForwardedIdentityV2Rejection
{
    /// <summary>Malformed envelope encoding.</summary>
    Format = 0,

    /// <summary>Unsupported envelope version.</summary>
    Version = 1,

    /// <summary>Timestamp outside the freshness window.</summary>
    Freshness = 2,

    /// <summary>Unrecognized signing key.</summary>
    Key = 3,

    /// <summary>Invalid request binding.</summary>
    Request = 4,

    /// <summary>Invalid covered fields.</summary>
    Headers = 5,

    /// <summary>Canonical input exceeds a bound.</summary>
    Bounds = 6,

    /// <summary>Signature verification failed.</summary>
    Signature = 7,

    /// <summary>Invalid principal or subject binding.</summary>
    Identity = 8,

    /// <summary>Invalid signed credential metadata.</summary>
    Metadata = 9,

    /// <summary>Bearer coverage or verification failed.</summary>
    Credential = 10,

    /// <summary>Replay reservation unavailable.</summary>
    Store = 11,

    /// <summary>Nonce previously reserved.</summary>
    Replay = 12
}
