// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_checking_can_handle;

public class with_events_and_concurrency_scopes : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    bool _result;

    void Because() => _result = _handler.CanHandle(
        _commandContext,
        new EventsWithConcurrencyScopes(
            [new(EventSourceId.New(), new FirstEvent("first"))],
            [new(EventSourceId.New(), ConcurrencyScope.None)]));

    [Fact] void should_be_able_to_handle() => _result.ShouldBeTrue();
}
