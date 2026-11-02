// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_reading_the_caller;

public class and_validation_nested_in_execute_observes_mutation : given.a_captured_caller
{
    void Because()
    {
        ((ClaimsIdentity)_principal.Identity).AddClaim(new Claim("login", "later"));
        using var validation = AuthorizationCommandIdentity.EnterForValidation(_principal, null, CommandCallerProvenance.Ambient);
        _success = AuthorizationCommandIdentity.TryGetCaller(_principal, out _caller, out _refusal);
    }

    [Fact] void should_refuse_the_caller() => _success.ShouldBeFalse();
    [Fact] void should_report_a_changed_caller() => _refusal.ShouldEqual(ReadModelKeyRefusal.CallerChanged);
}
