// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_injecting_a_reducer_backed_read_model;

public class into_a_nullable_handle_parameter_and_the_read_model_does_not_exist : Specification
{
    CommandScenario<UseNullableReducerReadModelInHandle> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<UseNullableReducerReadModelInHandle>();

        var readModels = Substitute.For<IReadModels>();
        readModels
            .GetInstanceById(typeof(ReducerAccountSummary), _eventSourceId, default)
            .Returns(Task.FromResult<object>(null!));

        _scenario.Services.Replace(ServiceDescriptor.Singleton<IReadModels>(readModels));
    }

    async Task Because() => _result = await _scenario.Execute(new UseNullableReducerReadModelInHandle(_eventSourceId));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_inject_null_for_the_missing_reducer_backed_read_model() =>
        await _scenario.ShouldHaveAppendedEvent<UseNullableReducerReadModelInHandle, ReducerReadModelAbsence>(_eventSourceId, @event => @event.WasAbsent);
}
