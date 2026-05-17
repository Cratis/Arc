// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_handler_takes_read_model_dependency;

public class and_command_is_executed : Specification
{
    CommandScenario<UseReadModelDependencyCommand> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;
    IReadModels _readModels;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<UseReadModelDependencyCommand>();

        _readModels = Substitute.For<IReadModels>();
        _readModels
            .GetInstanceById(typeof(AccountBalanceReadModel), Arg.Any<ReadModelKey>(), default)
            .Returns(Task.FromResult<object>(new AccountBalanceReadModel(42m)));

        _scenario.Services.AddSingleton(_readModels);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new UseReadModelDependencyCommand(_eventSourceId));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    [Fact]
    async Task should_append_event_with_value_from_read_model() =>
        await _scenario.ShouldHaveAppendedEvent<UseReadModelDependencyCommand, ReadModelDependencyUsed>(
            _eventSourceId,
            @event => @event.Balance == 42m);

    [Fact]
    void should_resolve_read_model_for_event_source_id() =>
        _readModels.ReceivedCalls().Any(_ =>
            _.GetMethodInfo().Name == nameof(IReadModels.GetInstanceById) &&
            _.GetArguments() is [Type readModelType, ReadModelKey key, _] &&
            readModelType == typeof(AccountBalanceReadModel) &&
            key.Value == _eventSourceId.Value).ShouldBeTrue();
}
