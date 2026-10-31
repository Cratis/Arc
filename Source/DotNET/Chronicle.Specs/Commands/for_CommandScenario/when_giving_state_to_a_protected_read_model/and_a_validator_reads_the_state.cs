// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_a_validator_reads_the_state : given.a_validated_pin
{
    async Task Because() => _result = await _scenario.Execute(new(_source, _state), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_validate_the_pinned_instance() => _result.ShouldBeSuccessful();
    [Fact] async Task should_pass_the_same_instance_to_handle() =>
        await _scenario.ShouldHaveAppendedEvent<DecideWithValidator, given.a_pinned_state.Decided>(_source, fact => fact.Status == "pinned");
}
