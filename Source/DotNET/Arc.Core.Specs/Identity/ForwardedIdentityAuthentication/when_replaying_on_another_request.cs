// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_replaying_on_another_request : given.a_signed_request
{
    ForwardedIdentityAuthenticationResult _result;

    void Because()
    {
        Authenticate().Principal.ShouldNotBeNull();
        _context.Items.Returns(new Dictionary<object, object?>());
        _result = Authenticate();
    }

    [Fact] void should_reject_the_replayed_envelope() => _result.Principal.ShouldBeNull();
    [Fact] void should_return_a_fixed_failure() => _result.Failure.ShouldEqual("forwarded-identity-rejected");
}
