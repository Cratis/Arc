// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_a_url_as_audience : given.a_builder
{
    void Establish() => _facts = _facts with { Audience = _facts.Resource };
    void Because() => Build();

    [Fact] void should_not_equate_audience_slug_and_resource_indicator() => _result.Refusal.ShouldEqual(CallerRefusal.AudienceMismatch);
    [Fact] void should_not_build_a_caller() => _result.Caller.ShouldBeNull();
}
