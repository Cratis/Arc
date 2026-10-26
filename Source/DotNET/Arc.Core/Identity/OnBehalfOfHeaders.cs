// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Headers Cratis AuthProxy forwards when a person or an agent acts on behalf of another user.
/// </summary>
/// <remarks>
/// The <c>x-ms-client-principal*</c> headers describe the represented user: the user whose roles and tenant
/// authorize the request. The <see cref="ActorHeader"/> describes who actually acts, and the <see cref="AssertionHeader"/>
/// is the signed token that binds the two. Arc only accepts them when <see cref="OnBehalfOfOptions.Enabled"/> is set and
/// the assertion verifies, and rejects the request otherwise.
/// </remarks>
public static class OnBehalfOfHeaders
{
    /// <summary>
    /// The header carrying the acting identity, as a base64 encoded UTF-8 JSON client principal in the same shape as
    /// <see cref="MicrosoftIdentityPlatformHeaders.PrincipalHeader"/>.
    /// </summary>
    public const string ActorHeader = "x-cratis-on-behalf-actor";

    /// <summary>
    /// The header carrying the RS256 signed assertion that binds the actor and the represented user's headers.
    /// </summary>
    public const string AssertionHeader = "x-cratis-on-behalf-assertion";
}
