// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_reading_the_caller;

public class and_a_claim_is_added_in_place_after_capture : given.a_captured_caller
{
    void Establish() => ((ClaimsIdentity)_principal.Identity).AddClaim(new Claim("login", "later"));
    void Because() => _success = AuthorizationCommandIdentity.TryGetCaller(_principal, out _caller, out _refusal);

    [Fact] void should_refuse_the_caller() => _success.ShouldBeFalse();
    [Fact] void should_report_a_changed_caller() => _refusal.ShouldEqual(ReadModelKeyRefusal.CallerChanged);
}
