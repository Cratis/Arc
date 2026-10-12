// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_constructing;

public class with_the_legacy_constructor : for_CommandTransactionAppender.given.a_definition_routed_command
{
    [Fact]
    async Task should_preserve_manual_construction_without_registry_validation()
    {
#pragma warning disable CS0618 // Verify the retained public constructor's compatibility behavior.
        var handler = new SingleEventForEventSourceIdCommandResponseValueHandler(_eventLog, Substitute.For<IEventTypes>(), Substitute.For<IConcurrencyScopeStrategies>());
#pragma warning restore CS0618
        CommandTransaction.Current = _unitOfWork;
        var result = await handler.Handle(_context, _invalid);
        result.IsSuccess.ShouldBeTrue();
        _unitOfWork.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "AddEvent").ShouldBeTrue();
    }
}
