// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_the_command_is_rejected : given.a_slice
{
    void Establish() => _slice.GivenReadModel(new LedgerBalance { Balance = 0m });

    async Task Because() => _outcome = await _slice.Run();

    [Fact] void should_allow_rejection_assertions() => _outcome.ShouldBeRejected();
    [Fact] void should_allow_exact_rejection_message_assertions() => _outcome.ShouldBeRejected("Ledger has no funds to settle.");
    [Fact] async Task should_allow_no_events_assertions() => await _outcome.ShouldNotHaveAppendedEvents();
    [Fact] async Task should_allow_no_events_assertions_for_the_source() => await _outcome.ShouldNotHaveAppendedEvents(_source);
}
