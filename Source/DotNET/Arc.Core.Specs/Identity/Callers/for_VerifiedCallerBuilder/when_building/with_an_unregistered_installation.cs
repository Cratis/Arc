// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_an_unregistered_installation : given.a_builder
{
    void Establish()
    {
        _installation = new("https://unregistered.example.test", "test");
        _facts = _facts with { Resource = _installation.Resource! };
    }
    void Because() => Build();

    [Fact] void should_refuse_an_unknown_resource_mapping() => _result.Refusal.ShouldEqual(CallerRefusal.InstallationNotConfigured);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
