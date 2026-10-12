// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender;

public class when_resolving_wrapper_handlers : given.a_definition_routed_command
{
    [Fact] async Task should_select_the_validating_single_wrapper_constructor() => await VerifyDiscovery<SingleEventForEventSourceIdCommandResponseValueHandler>(_invalid);
    [Fact] async Task should_select_the_validating_collection_constructor() => await VerifyDiscovery<EventsForEventSourceIdCommandResponseValueHandler>(new[] { _invalid });
    [Fact] async Task should_select_the_validating_exact_scopes_constructor() => await VerifyDiscovery<EventsWithConcurrencyScopesCommandResponseValueHandler>(new Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes([_invalid], []));

    async Task VerifyDiscovery<THandler>(object value)
        where THandler : class, ICommandResponseValueHandler
    {
        await using var services = ServicesFor<THandler>();
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICommandResponseValueHandler>().Returns([typeof(THandler)]);
        var handlers = new InstancesOf<ICommandResponseValueHandler>(types, services);
        var error = await Catch.Exception(() => handlers.Single().Handle(_context, value));
        error.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();

        // Explicit activations, unlike self-bindings, honor ActivatorUtilitiesConstructor.
        var activated = ActivatorUtilities.CreateInstance<THandler>(services);
        var activationError = await Catch.Exception(() => activated.Handle(_context, value));
        activationError.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
    }
}
