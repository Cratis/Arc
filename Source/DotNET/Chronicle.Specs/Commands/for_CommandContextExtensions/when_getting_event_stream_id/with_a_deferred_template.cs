// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions.when_getting_event_stream_id;

public class with_a_deferred_template : Specification
{
    CommandContext _context;
    TemplateCommand _command;
    EventStreamId? _first;
    EventStreamId? _second;

    void Establish()
    {
        _command = new();
        _context = new(CorrelationId.New(), typeof(TemplateCommand), _command, [], new EventStreamIdValuesProvider().Provide(_command), null);
    }

    void Because()
    {
        _first = _context.GetEventStreamId();
        _second = _context.GetEventStreamId();
    }

    [Fact] void should_resolve_the_template() => _first!.Value.ShouldEqual("reporting");
    [Fact] void should_keep_the_resolved_id() => _second.ShouldEqual(_first);
    [Fact] void should_read_the_part_only_once() => _command.Reads.ShouldEqual(1);

    [EventStreamId("{Scope}")]
    public class TemplateCommand
    {
        public int Reads { get; private set; }
        public string Scope
        {
            get
            {
                Reads++;
                return "reporting";
            }
        }
    }
}
