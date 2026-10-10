// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.given;

public class a_command_pipeline_with_tag_handlers : a_command_pipeline_with_event_handlers_and_command
{
    protected NamedTag[] _tags;
    System.Diagnostics.ActivitySource _source;

    void Establish()
    {
        _tags = [new("project", "one")];
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _singleEventHandler = new(_eventLog, _eventTypes, _concurrencyScopeStrategies);
        _eventsHandler = new(_eventLog, _eventTypes, _concurrencyScopeStrategies);
        var handlers = new CommandResponseValueHandlers(new KnownInstancesOf<ICommandResponseValueHandler>([_singleEventHandler, _eventsHandler, new EventTagsCommandResponseValueHandler()]));
        var activity = Substitute.For<IActivitySource<CommandPipeline>>();
        _source = new("Arc.TagSpecs");
        activity.ActualSource.Returns(_source);
        _commandPipeline = new(_correlationIdAccessor, _commandFilters, _commandHandlerProviders, handlers, _commandContextModifier, _commandContextValuesBuilder, _commandHandlerArgumentResolver, new KnownInstancesOf<ICommandExecutionScope>([]), _serviceScopeFactory, activity);
        _eventLog.AppendWithNamedTags(Arg.Any<EventSourceId>(), Arg.Any<object>(), Arg.Any<IEnumerable<NamedTag>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Subject?>()).Returns(AppendResult.Success(_correlationId, EventSequenceNumber.First));
    }

    void Destroy() => _source.Dispose();
}
