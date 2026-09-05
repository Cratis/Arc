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

public class into_a_command_handle : Specification
{
    CommandScenario<UseReducerReadModelInHandle> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<UseReducerReadModelInHandle>();

        var readModels = Substitute.For<IReadModels>();
        readModels
            .GetInstanceById(typeof(ReducerAccountSummary), _eventSourceId, default)
            .Returns(Task.FromResult<object>(new ReducerAccountSummary(250m, false)));

        _scenario.Services.Replace(ServiceDescriptor.Singleton<IReadModels>(readModels));
    }

    async Task Because() => _result = await _scenario.Execute(new UseReducerReadModelInHandle(_eventSourceId));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_have_appended_the_value_from_the_reducer_backed_read_model() =>
        await _scenario.ShouldHaveAppendedEvent<UseReducerReadModelInHandle, ReducerBalanceUsedInHandle>(_eventSourceId, @event => @event.Balance == 250m);
}
