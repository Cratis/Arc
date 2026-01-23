// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.given;

public class a_command_pipeline_with_event_handlers : Specification
{
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected ICommandFilters _commandFilters;
    protected ICommandHandlerProviders _commandHandlerProviders;
    protected ICommandResponseValueHandlers _commandResponseValueHandlers;
    protected ICommandContextModifier _commandContextModifier;
    protected ICommandContextValuesBuilder _commandContextValuesBuilder;
    protected IServiceProvider _serviceProvider;
    protected IEventLog _eventLog;
    protected IEventTypes _eventTypes;
    protected CommandPipeline _commandPipeline;
    protected CorrelationId _correlationId;
    protected SingleEventCommandResponseValueHandler _singleEventHandler;
    protected EventsCommandResponseValueHandler _eventsHandler;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _correlationIdAccessor.Current.Returns(_correlationId);
        _commandFilters = Substitute.For<ICommandFilters>();
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Success(_correlationId));
        _commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        _commandContextModifier = Substitute.For<ICommandContextModifier>();
        _commandContextValuesBuilder = Substitute.For<ICommandContextValuesBuilder>();
        _serviceProvider = Substitute.For<IServiceProvider>();

        // Set up event handling infrastructure
        _eventLog = Substitute.For<IEventLog>();
        _eventTypes = Substitute.For<IEventTypes>();
        _singleEventHandler = new SingleEventCommandResponseValueHandler(_eventLog, _eventTypes);
        _eventsHandler = new EventsCommandResponseValueHandler(_eventLog, _eventTypes);

        // Set up successful append results
        var successfulAppendResult = AppendResult.Success(_correlationId, EventSequenceNumber.First);
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<ConcurrencyScope?>()).Returns(successfulAppendResult);

        var successfulAppendManyResult = AppendManyResult.Success(_correlationId, []);
        _eventLog.AppendMany(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<object>>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<ConcurrencyScope?>()).Returns(successfulAppendManyResult);

        // Create a command response value handlers that includes event handlers
        _commandResponseValueHandlers = Substitute.For<ICommandResponseValueHandlers>();
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>())
            .Returns(callInfo =>
            {
                var ctx = callInfo.ArgAt<CommandContext>(0);
                var value = callInfo.ArgAt<object>(1);
                return _singleEventHandler.CanHandle(ctx, value) || _eventsHandler.CanHandle(ctx, value);
            });

        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), Arg.Any<object>())
            .Returns(async callInfo =>
            {
                var ctx = callInfo.ArgAt<CommandContext>(0);
                var value = callInfo.ArgAt<object>(1);
                if (_singleEventHandler.CanHandle(ctx, value))
                {
                    return await _singleEventHandler.Handle(ctx, value);
                }
                if (_eventsHandler.CanHandle(ctx, value))
                {
                    return await _eventsHandler.Handle(ctx, value);
                }
                return CommandResult.Success(_correlationId);
            });

        _commandPipeline = new(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder);
    }

    protected record TestEvent(string Name);
    protected record AnotherTestEvent(int Value);
}
