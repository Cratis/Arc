// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_given_a_read_model : given.a_slice
{
    void Establish() => _slice.GivenReadModel(new LedgerBalance { Balance = 42m });

    async Task Because() => _outcome = await _slice.Run();

    [Fact] void should_use_the_pinned_read_model() => _outcome.ShouldBeSuccessful();
    [Fact] async Task should_pass_the_model_to_the_handler() => await _outcome.ShouldHaveAppendedEventsInOrder(new LedgerSettled(42m));
}
