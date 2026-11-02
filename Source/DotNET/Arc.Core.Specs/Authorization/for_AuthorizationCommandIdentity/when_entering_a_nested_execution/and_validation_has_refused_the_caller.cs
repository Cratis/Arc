// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_entering_a_nested_execution;

public class and_validation_has_refused_the_caller : given.a_captured_caller
{
    Exception _exception;

    void Establish() => AuthorizationCommandIdentity.MarkTransactional();

    void Because()
    {
        var identity = (ClaimsIdentity)_principal.Identity;
        var added = new Claim("login", "later");
        identity.AddClaim(added);
        using var validation = AuthorizationCommandIdentity.EnterForValidation(_principal, null, CommandCallerProvenance.Ambient);
        identity.RemoveClaim(identity.FindFirst("login"));
        _exception = Catch.Exception(() =>
        {
            using var execution = AuthorizationCommandIdentity.Enter(_principal);
        });
    }

    [Fact] void should_preserve_the_refusal() => _exception.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
}
