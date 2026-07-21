// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

/// <summary>
/// A command composes its own key from its properties, so partial or hostile input leaves it composing a key that arrived
/// null — here through the implicit conversion on a generic <see cref="EventSourceId{T}"/>, which is how it surfaces in
/// practice. This runs while the command context is built, before any command filter, so the throw would otherwise escape
/// as an unhandled server error instead of reaching the stages that turn bad input into a clean response.
/// </summary>
/// <remarks>
/// Degrading silently would hide a genuine defect inside an implementation, so the swallow has to leave a trace — at debug
/// level, since the usual cause is bad input and a hostile caller must not be able to flood the log.
/// </remarks>
public class with_a_command_that_provides_a_key_it_cannot_compose : Specification
{
    RecordingLogger<EventSourceValuesProvider> _logger;
    EventSourceValuesProvider _provider;
    CommandContextValues _result;
    Exception _exception;

    void Establish()
    {
        _logger = new RecordingLogger<EventSourceValuesProvider>();
        _provider = new EventSourceValuesProvider(_logger);
    }

    void Because() => _exception = Catch.Exception(() => _result = _provider.Provide(new CommandThatCannotComposeItsKey(null!)));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_provide_an_event_source_id() => _result.ContainsKey(WellKnownCommandContextKeys.EventSourceId).ShouldBeTrue();
    [Fact] void should_provide_an_unspecified_event_source_id() =>
        ((EventSourceId)_result[WellKnownCommandContextKeys.EventSourceId]).ShouldEqual(EventSourceId.Unspecified);

    [Fact] void should_log_that_the_key_could_not_be_composed() => _logger.Entries.Count.ShouldEqual(1);
    [Fact] void should_log_at_debug_level() => _logger.Entries[0].Level.ShouldEqual(LogLevel.Debug);
    [Fact] void should_log_the_underlying_failure() => _logger.Entries[0].Exception.ShouldNotBeNull();

    record CommandThatCannotComposeItsKey(EventSourceId<Guid> Id) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => Id;
    }
}
