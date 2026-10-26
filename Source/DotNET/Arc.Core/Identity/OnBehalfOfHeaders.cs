// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Headers a trusted Cratis AuthProxy forwards when a person or an agent acts on behalf of another user.
/// </summary>
/// <remarks>
/// The <c>x-ms-client-principal*</c> headers describe the represented user: the user whose roles and tenant
/// authorize the request. The <see cref="ActorHeader"/> describes who actually acts. Arc only accepts it from a
/// trusted forward and rejects the request otherwise.
/// </remarks>
public static class OnBehalfOfHeaders
{
    /// <summary>
    /// The header carrying the acting identity, as a base64 encoded UTF-8 JSON client principal in the same shape as
    /// <see cref="MicrosoftIdentityPlatformHeaders.PrincipalHeader"/>.
    /// </summary>
    public const string ActorHeader = "x-cratis-on-behalf-actor";
}
