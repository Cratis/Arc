// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_entering_a_nested_execution;

public class and_validation_hides_an_active_transaction : given.a_captured_caller
{
    Exception _exception;

    void Establish() => AuthorizationCommandIdentity.MarkTransactional();

    void Because()
    {
        using var validation = AuthorizationCommandIdentity.EnterForValidation(_principal, null, CommandCallerProvenance.Ambient);
        var other = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "other")], "test"));
        _exception = Catch.Exception(() =>
        {
            using var execution = AuthorizationCommandIdentity.Enter(other);
        });
    }

    [Fact] void should_refuse_the_identity_change() => _exception.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
}
