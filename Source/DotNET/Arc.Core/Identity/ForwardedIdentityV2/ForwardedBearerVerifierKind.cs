// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>The independently selected bearer verifier.</summary>
internal enum ForwardedBearerVerifierKind
{
    /// <summary>The local PAT verifier.</summary>
    Pat = 0,

    /// <summary>The OAuth verifier.</summary>
    OAuth = 1
}
