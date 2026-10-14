// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_a_competing_append : given.a_direct_write_scenario<DirectWrite>
{
    void Establish() => _scenario.AppendConcurrently(_id, new Created("competitor"));
    async Task Because() => await Execute(new(WriteStyle.AppendMany));

    [Fact] void should_reject_at_commit() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_classify_the_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_keep_only_the_competitors_event() => _after.ShouldEqual(EventSequenceNumber.First);
}
