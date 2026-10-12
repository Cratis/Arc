// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_an_undeclared_legacy_stream : for_CommandTransactionAppender.given.a_definition_routed_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_refuse_before_any_effect(bool transaction)
    {
        await using var services = ServicesFor<SingleEventForEventSourceIdCommandResponseValueHandler>();
        var handler = services.GetRequiredService<SingleEventForEventSourceIdCommandResponseValueHandler>();
        CommandTransaction.Current = transaction ? _unitOfWork : null;
        var error = await Catch.Exception(() => handler.Handle(_context, _invalid));
        error.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
        ShouldHaveNoEffects();
    }
}
