// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.for_SpecificationSyntaxBuilder.when_building;

public class a_caller_with_repeated_roles_and_claims : Specification
{
    SpecificationCallerSyntax _caller;

    void Because()
    {
        var specification = new SpecificationModel("Caller", [], null, [], [])
        {
            Caller = new(true, ["Staff", "Staff", "Other"], [new("Tenant", "a"), new("tenant", "b"), new("Tenant", "a")])
        };
        _caller = new SpecificationSyntaxBuilder(new ScreenplayNaming()).Build([specification]).Single().GivenCaller!;
    }

    [Fact] void should_state_authentication() => _caller.Authenticated.ShouldBeTrue();
    [Fact] void should_preserve_roles_including_repetition() => _caller.Roles.ShouldEqual(["Staff", "Staff", "Other"]);
    [Fact] void should_preserve_claim_order_and_casing() => _caller.Claims.Select(claim => KeyValuePair.Create(claim.Type, claim.Value)).ShouldEqual([new("Tenant", "a"), new("tenant", "b"), new("Tenant", "a")]);
}
