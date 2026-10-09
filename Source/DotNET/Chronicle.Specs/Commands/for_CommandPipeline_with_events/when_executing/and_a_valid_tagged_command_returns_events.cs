// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_a_valid_tagged_command_returns_events : given.a_pipeline_with_deferred_tags
{
    CommandResult _result;
    void Establish()
    {
        _taggedCommand = _taggedCommand with { ProjectId = "one" };
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(_taggedCommand, out anyHandler).Returns(call =>
        {
            call[1] = _commandHandler;
            return true;
        });
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(new object[] { new TestEvent("first"), new TestEvent("second") });
        _eventLog.AppendManyWithNamedTags(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IEnumerable<NamedTag>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Subject?>()).Returns(AppendManyResult.Success(_correlationId, []));
    }
    async Task Because() => _result = await _commandPipeline.Execute(_taggedCommand, _serviceProvider);

    [Fact] void should_append_the_resolved_tag_union() => _eventLog.Received(1).AppendManyWithNamedTags(_taggedCommand.Id, Arg.Any<IEnumerable<object>>(), Arg.Is<IEnumerable<NamedTag>>(_ => _.SequenceEqual(new NamedTag[] { new("project", "one"), new("tenant", "one") })));
    [Fact] void should_evaluate_application_providers_once() => _tagProvider.Received(1).GetEventTags(_taggedCommand);
    [Fact] void should_evaluate_command_providers_once() => _taggedCommand.TagReads.ShouldEqual(1);
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_store_resolved_tags_as_returned_tags() => _builtValues.ContainsKey(WellKnownCommandContextKeys.EventTags).ShouldBeFalse();
}
