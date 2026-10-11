// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventCommandResponseValueHandler;

public class when_declaring_attributed_response_values : Specification
{
    Type[] _contracts;

    void Because() => _contracts = typeof(SingleEventCommandResponseValueHandler).GetInterfaces();

    [Fact] void should_declare_event_types_as_consumed() => _contracts.ShouldContain(typeof(ICommandResponseValueHandlerForAttributed<EventTypeAttribute>));
    [Fact] void should_not_declare_event_enumerables() => _contracts.ShouldNotContain(typeof(ICommandResponseValueHandlerForAttributedEnumerable<EventTypeAttribute>));
}
