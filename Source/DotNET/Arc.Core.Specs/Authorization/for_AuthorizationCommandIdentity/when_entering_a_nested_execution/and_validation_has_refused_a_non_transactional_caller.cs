// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_entering_a_nested_execution;

public class and_validation_has_refused_a_non_transactional_caller : given.a_captured_caller
{
    Exception _exception;

    void Because()
    {
        var identity = (ClaimsIdentity)_principal.Identity;
        identity.AddClaim(new Claim("login", "later"));
        using var validation = AuthorizationCommandIdentity.EnterForValidation(_principal, null, CommandCallerProvenance.Ambient);
        identity.RemoveClaim(identity.FindFirst("login"));
        _exception = Catch.Exception(() =>
        {
            using var execution = AuthorizationCommandIdentity.Enter(_principal);
        });
    }

    [Fact] void should_preserve_the_refusal() => _exception.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_report_a_safe_non_transactional_reason() => _exception.Message.ShouldEqual("A nested command cannot execute after the caller identity was refused.");
}
