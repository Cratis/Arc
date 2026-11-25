// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_the_host_is_not_a_development_environment : given.a_development_host
{
    Exception _error;
    AvailabilityDecision _decision;

    protected override string Environment => "Production";

    async Task Because()
    {
        _error = await Catch.Exception(async () => await _development.Apply(AvailabilityOverrides.None with { EnabledAudiences = ["Plus"] }));
        _decision = await Decide(typeof(PlusCommand));
    }

    [Fact] void should_refuse_the_overrides() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_be_active() => _development.IsActive.ShouldBeFalse();
    [Fact] void should_apply_nothing() => _development.Current.IsEmpty.ShouldBeTrue();
    [Fact] void should_decide_from_the_gates_alone() => _decision.IsAvailable.ShouldBeFalse();
}
