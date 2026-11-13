// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>A presentation dispatch hint, never verification of the credential.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal enum PresentedCredentialShape
{
    /// <summary>A signed forwarded-identity envelope.</summary>
    Envelope = 0,

    /// <summary>A bearer in a product PAT namespace; never fall back to OAuth on refusal.</summary>
    PatBearer = 1,

    /// <summary>A bearer outside a PAT namespace.</summary>
    OAuthBearer = 2,

    /// <summary>A bearer explicitly routed by server-owned workload endpoint metadata.</summary>
    ServiceAccount = 3
}
