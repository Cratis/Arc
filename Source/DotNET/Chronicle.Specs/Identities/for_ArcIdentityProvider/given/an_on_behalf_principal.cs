// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.given;

public class an_on_behalf_principal : an_identity_provider
{
    protected static readonly ChronicleIdentity _actor = new("support-123", "Support", "support");
    protected static readonly ChronicleIdentity _represented = new("person-123", "Person", "person");
    protected ClaimsPrincipal _onBehalfPrincipal;

    void Establish()
    {
        var represented = new ClaimsIdentity([new Claim("sub", "person-123"), new Claim("name", "Person"), new Claim("preferred_username", "person")], "test")
        {
            Actor = new ClaimsIdentity([new Claim("sub", "support-123"), new Claim("name", "Support"), new Claim("preferred_username", "support")], "test")
        };
        _onBehalfPrincipal = new(represented);
    }
}
