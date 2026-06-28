// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_instance.into_a_command_validator;

public class and_state_allows_command : Specification
{
    CommandScenario<WithdrawFunds> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<WithdrawFunds>();
        _scenario.Given.ForEventSource(_eventSourceId).ReadModel(new AccountBalanceReadModel(100m));
    }

    async Task Because() => _result = await _scenario.Execute(new WithdrawFunds(_eventSourceId));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_have_appended_the_event() => await _scenario.ShouldHaveAppendedEvent<WithdrawFunds, FundsWithdrawn>(_eventSourceId);
}
