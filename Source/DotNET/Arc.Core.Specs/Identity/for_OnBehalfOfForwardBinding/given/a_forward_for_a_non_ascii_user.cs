// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_OnBehalfOfForwardBinding.given;

public class a_forward_for_a_non_ascii_user : Specification
{
    /// <summary>
    /// A user id a header field cannot carry verbatim.
    /// </summary>
    protected const string UserId = "søren-wærstad";

    /// <summary>
    /// The user id in the RFC 8187 form AuthProxy puts on <c>x-ms-client-principal-id</c>.
    /// </summary>
    protected const string EncodedUserId = "UTF-8''s%C3%B8ren-w%C3%A6rstad";

    protected const string PrincipalHeader = "principal-header";
    protected const string ActorHeader = "actor-header";

    internal OnBehalfOfAssertion _assertion;
    protected ClientPrincipal _represented;

    void Establish()
    {
        _assertion = new(
            UserId,
            "support-person-id",
            null,
            OnBehalfOfBase64Url.Sha256Of(PrincipalHeader),
            OnBehalfOfBase64Url.Sha256Of(ActorHeader),
            "GET",
            "/api/tickets",
            "token-id",
            DateTimeOffset.UtcNow.AddMinutes(1));
        _represented = new() { UserId = UserId };
    }

    internal static OnBehalfOfForward ForwardWithPrincipalId(string principalId) =>
        new(PrincipalHeader, principalId, ActorHeader, "assertion", null, "GET", "/api/tickets");
}
