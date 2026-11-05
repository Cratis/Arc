// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>The independent source of bearer facts, when present.</summary>
internal enum ForwardedIdentityV2CallerSource
{
    /// <summary>No bearer was forwarded.</summary>
    None = 0,

    /// <summary>The PAT verifier supplied facts.</summary>
    Pat = 1,

    /// <summary>The OAuth verifier supplied facts.</summary>
    OAuth = 2
}
