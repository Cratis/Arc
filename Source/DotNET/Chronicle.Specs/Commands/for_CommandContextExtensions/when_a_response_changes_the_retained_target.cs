// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions;

public class when_a_response_changes_the_retained_target : Specification
{
    CommandContext _context;
    EventSourceId _response;
    object? _raw;

    void Establish()
    {
        var command = new CommandWithKey("original");
        _response = "response-target";
        var values = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance).Provide(command);
        _context = new(CorrelationId.NotSet, command.GetType(), command, [], values, Response: _response);
    }

    void Because() => _raw = _context.GetRawEventSourceKey();
    [Fact] void should_use_the_response_instead_of_the_old_raw_key() => _raw.ShouldBeSame(_response);
    [Fact] void should_not_change_the_resolved_target() => _context.GetEventSourceId().ShouldEqual(_response);

    record CommandWithKey([Key] string Id);
}
