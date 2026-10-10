// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Streams.for_EventRoute.when_resolving;

public class an_unknown_stream : Specification
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => EventRoute.For(new EventSourceDefinition(typeof(UnknownStreamReport), "reports", "", ConcurrencyDimensions.None, []), "missing"));
    [Fact] void should_preserve_the_values_provider_exception() => _error.ShouldBeOfExactType<Commands.EventRoutingContradictsEventSource>();
    class UnknownStreamReport : IEventSource;
}
