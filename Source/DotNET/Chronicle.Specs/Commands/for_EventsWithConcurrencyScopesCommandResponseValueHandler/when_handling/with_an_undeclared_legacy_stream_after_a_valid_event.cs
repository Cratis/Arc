// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_an_undeclared_legacy_stream_after_a_valid_event : for_CommandTransactionAppender.given.a_definition_routed_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_refuse_before_enrolling_the_batch(bool transaction)
    {
        await using var services = ServicesFor<EventsWithConcurrencyScopesCommandResponseValueHandler>();
        var handler = services.GetRequiredService<EventsWithConcurrencyScopesCommandResponseValueHandler>();
        CommandTransaction.Current = transaction ? _unitOfWork : null;
        var error = await Catch.Exception(() => handler.Handle(_context, new EventsWithConcurrencyScopes([_valid, _invalid], [])));
        error.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
        ShouldHaveNoEffects();
    }
}
