// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler;

public class when_declaring_attributed_response_values : Specification
{
    Type[] _contracts;

    void Because() => _contracts = typeof(EventsCommandResponseValueHandler).GetInterfaces();

    [Fact] void should_declare_event_enumerables_as_consumed() => _contracts.ShouldContain(typeof(ICommandResponseValueHandlerForAttributedEnumerable<EventTypeAttribute>));
    [Fact] void should_not_declare_scalar_event_values() => _contracts.ShouldNotContain(typeof(ICommandResponseValueHandlerForAttributed<EventTypeAttribute>));
}
