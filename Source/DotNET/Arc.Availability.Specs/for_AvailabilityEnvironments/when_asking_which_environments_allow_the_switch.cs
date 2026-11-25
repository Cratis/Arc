// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEnvironments;

public class when_asking_which_environments_allow_the_switch : Specification
{
    [Fact] void should_allow_development() => AvailabilityEnvironments.AllowsGrantAll("Development").ShouldBeTrue();
    [Fact] void should_allow_local_in_any_case() => AvailabilityEnvironments.AllowsGrantAll("local").ShouldBeTrue();
    [Fact] void should_allow_test() => AvailabilityEnvironments.AllowsGrantAll("Test").ShouldBeTrue();
    [Fact] void should_allow_testing() => AvailabilityEnvironments.AllowsGrantAll("Testing").ShouldBeTrue();
    [Fact] void should_refuse_production() => AvailabilityEnvironments.AllowsGrantAll("Production").ShouldBeFalse();
    [Fact] void should_refuse_staging() => AvailabilityEnvironments.AllowsGrantAll("Staging").ShouldBeFalse();
    [Fact] void should_refuse_an_empty_name() => AvailabilityEnvironments.AllowsGrantAll(string.Empty).ShouldBeFalse();
    [Fact] void should_refuse_no_name() => AvailabilityEnvironments.AllowsGrantAll(null).ShouldBeFalse();
}
