// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_named_metadata : given.a_direct_write_scenario<DirectWrite>
{
    AppendedEvent[] _stored;
    async Task Because()
    {
        await Execute(new(WriteStyle.AppendManyWithNamedTags));
        _stored = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_event_order() => _stored.Select(@event => @event.Content).ShouldEqual([new Created("first"), new Created("second")]);
    [Fact] void should_preserve_occurred_time() => _stored.ShouldEachConformTo(@event => @event.Context.Occurred == Occurred);
    [Fact] void should_preserve_subject() => _stored.ShouldEachConformTo(@event => @event.Context.Subject == Subject);
    [Fact] void should_preserve_named_tags() => _stored.ShouldEachConformTo(@event => @event.Context.NamedTags.SequenceEqual(NamedTags));
}
