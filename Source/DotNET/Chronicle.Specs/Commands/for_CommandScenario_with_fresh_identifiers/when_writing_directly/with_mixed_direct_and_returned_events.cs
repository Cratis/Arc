// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_mixed_direct_and_returned_events : given.a_direct_write_scenario<MixedDirectWrite>
{
    async Task Because() => await Execute(new());

    [Fact] void should_share_one_guard() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_all_three_events() => _after.ShouldEqual(EventSequenceNumber.First + 2);
}
