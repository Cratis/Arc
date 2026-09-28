// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity;

public class when_a_custom_identity_changes_only_opaque_state : Specification
{
    bool _same;

    void Because()
    {
        var identity = new CustomIdentity([new Claim("permission", "reader")], "test");
        var principal = new ClaimsPrincipal(identity);
        var snapshot = AuthorizationPrincipalIdentity.Capture(principal);
        identity.OpaquePermission = "writer";
        _same = AuthorizationPrincipalIdentity.Same(snapshot, principal);
    }

    [Fact] void should_document_that_hidden_custom_state_is_not_detected() => _same.ShouldBeTrue();

    sealed class CustomIdentity(IEnumerable<Claim> claims, string authenticationType) : ClaimsIdentity(claims, authenticationType)
    {
        public string OpaquePermission { get; set; } = "reader";
    }
}
