// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Secret-free revocation outcomes. Refusals never disclose another person's credential.</summary>
public enum PatRevokeResult
{
    /// <summary>The owned credential was revoked.</summary>
    Revoked = 0,

    /// <summary>The owned credential was already revoked.</summary>
    AlreadyRevoked = 1,

    /// <summary>The credential is not available to this verified owner.</summary>
    Refused = 2,

    /// <summary>Current authoritative state could not be established.</summary>
    Unavailable = 3
}
