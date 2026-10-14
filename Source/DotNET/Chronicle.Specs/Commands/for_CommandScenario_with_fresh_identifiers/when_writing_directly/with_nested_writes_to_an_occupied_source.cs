// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_nested_writes_to_an_occupied_source : given.a_direct_write_scenario<NestedDirectWrite>
{
    void Establish() => SeedOccupiedSource();
    async Task Because() => await Execute(new());

    [Fact] void should_fail_the_outer_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_concurrency_violation() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_classify_the_outer_capture_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_append_nothing() => _after.ShouldEqual(_before);
}
