// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute.Core;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_provide_takes_read_model_dependency;

public class and_command_is_executed : Specification
{
    CommandScenario<ProvideReadModelDependencyCommand> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;
    IReadModels _readModels;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<ProvideReadModelDependencyCommand>();

        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        clientArtifactsProvider.Projections.Returns([typeof(AccountBalanceReadModelProjection)]);
        clientArtifactsProvider.ModelBoundProjections.Returns([]);
        _scenario.Services.AddReadModels(clientArtifactsProvider);

        _readModels = Substitute.For<IReadModels>();
        _readModels
            .GetInstanceById(typeof(AccountBalanceReadModel), _eventSourceId, default)
            .Returns(Task.FromResult<object>(new AccountBalanceReadModel(42m)));
        _scenario.Services.Replace(new ServiceDescriptor(typeof(IReadModels), _readModels));
    }

    async Task Because() => _result = await _scenario.Execute(new ProvideReadModelDependencyCommand(_eventSourceId));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_resolve_the_read_model_for_the_command_event_source_id() =>
        _readModels.ReceivedCalls().Count(IsReadModelLookupForCommandEventSourceId).ShouldEqual(1);

    [Fact]
    async Task should_append_event_with_value_provided_from_read_model() =>
        await _scenario.ShouldHaveAppendedEvent<ProvideReadModelDependencyCommand, ReadModelDependencyProvided>(
            _eventSourceId,
            @event => @event.Balance == 42m);

    bool IsReadModelLookupForCommandEventSourceId(ICall call) =>
        call.GetMethodInfo().Name == nameof(IReadModels.GetInstanceById) &&
        call.GetArguments() is [Type readModelType, ReadModelKey readModelKey, _] &&
        readModelType == typeof(AccountBalanceReadModel) &&
        readModelKey.Value == _eventSourceId.Value;
}
