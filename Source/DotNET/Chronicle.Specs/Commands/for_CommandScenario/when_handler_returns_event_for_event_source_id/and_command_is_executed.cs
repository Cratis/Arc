// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_handler_returns_event_for_event_source_id;

public class and_command_is_executed : Specification
{
    CommandScenario<TransferFundsCommand> _scenario;
    CommandResult _result;
    EventSourceId _fromAccountId;
    EventSourceId _toAccountId;

    void Establish()
    {
        _fromAccountId = EventSourceId.New();
        _toAccountId = EventSourceId.New();
        _scenario = new CommandScenario<TransferFundsCommand>();
    }

    async Task Because() =>
        _result = await _scenario.Execute(new TransferFundsCommand(_fromAccountId, _toAccountId, 500));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_have_appended_funds_debited_event() =>
        await _scenario.ShouldHaveAppendedEvent<TransferFundsCommand, FundsDebited>(_fromAccountId);
    [Fact] async Task should_have_appended_event_with_correct_amount() =>
        await _scenario.ShouldHaveAppendedEvent<TransferFundsCommand, FundsDebited>(
            _fromAccountId,
            e => e.Amount == 500);
}
