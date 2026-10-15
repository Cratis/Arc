// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_a_result_carries_its_own_event_sources : given.a_command_pipeline_with_event_handlers
{
    [Theory]
    [InlineData(typeof(OneEvent), 1)]
    [InlineData(typeof(Many), 2)]
    [InlineData(typeof(AsyncOneEvent), 1)]
    [InlineData(typeof(AsyncMany), 2)]
    public async Task should_append_to_the_wrappers_without_a_command_event_source_provider(Type commandType, int expectedEvents)
    {
        var command = Activator.CreateInstance(commandType, (EventSourceId)"first", (EventSourceId)"second")!;
        var handler = new ModelBoundCommandHandler(commandType, commandType.GetMethod("Handle")!);
        _commandHandlerProviders.TryGetHandlerFor(command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = handler;
            return true;
        });
        _commandContextValuesBuilder.Build(command).Returns(new CommandContextValues());
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(AppendManyResult.Success(_correlationId, []));

        var result = await _commandPipeline.Execute(command, _serviceProvider);

        Assert.False(result.HasExceptions, string.Join(Environment.NewLine, result.ExceptionMessages));
        result.IsSuccess.ShouldBeTrue();
        var appended = _eventLog.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == "Append" || _.GetMethodInfo().Name == "AppendMany").ToArray();
        appended.Length.ShouldEqual(1);
        if (expectedEvents == 1)
        {
            appended[0].GetArguments()[0].ShouldEqual((EventSourceId)"first");
        }
        else
        {
            ((IEnumerable<EventForEventSourceId>)appended[0].GetArguments()[0]!).Select(_ => _.EventSourceId.Value).ShouldEqual(["first", "second"]);
        }
    }

    [Command]
    public record OneEvent(EventSourceId First, EventSourceId Second)
    {
        public Result<EventForEventSourceId, ValidationResult> Handle() => new EventForEventSourceId(First, new TestEvent("single"));
    }

    [Command]
    public record Many(EventSourceId First, EventSourceId Second)
    {
        public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle() => new[]
        {
            new EventForEventSourceId(First, new TestEvent("first")),
            new EventForEventSourceId(Second, new TestEvent("second"))
        };
    }

    [Command]
    public record AsyncOneEvent(EventSourceId First, EventSourceId Second)
    {
        public Task<Result<EventForEventSourceId, ValidationResult>> Handle() => Task.FromResult(new OneEvent(First, Second).Handle());
    }

    [Command]
    public record AsyncMany(EventSourceId First, EventSourceId Second)
    {
        public Task<Result<IEnumerable<EventForEventSourceId>, ValidationResult>> Handle() => Task.FromResult(new Many(First, Second).Handle());
    }
}
