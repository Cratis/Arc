// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_constructing;

public class with_the_legacy_constructor : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    [Fact]
    async Task should_preserve_manual_construction_without_registry_validation()
    {
#pragma warning disable CS0618 // Verify the retained public constructor's compatibility behavior.
        var handler = new EventsWithConcurrencyScopesCommandResponseValueHandler(_eventLog);
#pragma warning restore CS0618
        _commandContext.Values[WellKnownCommandContextKeys.EventSource] = typeof(Account);
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceType] = (EventSourceType)"Account";
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamType] = (EventStreamType)"Transactions";
        CommandTransaction.Current = _unitOfWork;
        var wrapper = new EventForEventSourceId("id", new object()) { EventSourceType = "Account", EventStreamType = "Typo" };
        var result = await handler.Handle(_commandContext, new EventsWithConcurrencyScopes([wrapper], []));
        result.IsSuccess.ShouldBeTrue();
        _unitOfWork.AddEventsCallCount.ShouldEqual(1);
    }

    public class Account;
}
