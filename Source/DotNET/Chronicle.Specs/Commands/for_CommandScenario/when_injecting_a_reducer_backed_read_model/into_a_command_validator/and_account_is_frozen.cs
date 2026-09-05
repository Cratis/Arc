// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_injecting_a_reducer_backed_read_model.into_a_command_validator;

public class and_account_is_frozen : Specification
{
    CommandScenario<WithdrawFromReducerAccount> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<WithdrawFromReducerAccount>();

        var readModels = Substitute.For<IReadModels>();
        readModels
            .GetInstanceById(typeof(ReducerAccountSummary), _eventSourceId, default)
            .Returns(Task.FromResult<object>(new ReducerAccountSummary(100m, true)));

        _scenario.Services.Replace(ServiceDescriptor.Singleton<IReadModels>(readModels));
    }

    async Task Because() => _result = await _scenario.Execute(new WithdrawFromReducerAccount(_eventSourceId));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_validation_errors() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_validation_error_from_the_reducer_backed_read_model() => _result.ValidationResults.First().Message.ShouldEqual("Account is frozen.");
    [Fact] void should_not_have_appended_events() => _scenario.AppendedEvents.ShouldBeEmpty();
}
