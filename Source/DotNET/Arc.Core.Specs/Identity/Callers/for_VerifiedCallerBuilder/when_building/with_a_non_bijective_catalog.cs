// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_a_non_bijective_catalog : given.a_builder
{
    void Establish() => _catalog = new(new Dictionary<string, string>
    {
        { _facts.Resource, "example-api" },
        { "https://another.example.test", "example-api" }
    });
    void Because() => Build();

    [Fact] void should_require_one_to_one_resource_audience_mapping() => _result.Refusal.ShouldEqual(CallerRefusal.InstallationNotConfigured);
    [Fact] void should_not_build_a_caller() => _result.Caller.ShouldBeNull();
}
