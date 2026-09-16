// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_an_empty_response_in_a_transaction : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    CommandResult _result;

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        _result = await _handler.Handle(_commandContext, new EventsWithConcurrencyScopes([], []));
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_command_correlation() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_enqueue_an_empty_batch() => _unitOfWork.AddEventsCallCount.ShouldEqual(0);
}
