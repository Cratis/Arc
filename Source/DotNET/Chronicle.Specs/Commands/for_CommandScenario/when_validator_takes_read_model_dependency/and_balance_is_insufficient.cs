// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_validator_takes_read_model_dependency;

public class and_balance_is_insufficient : Specification
{
    CommandScenario<WithdrawFunds> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<WithdrawFunds>();

        var readModels = Substitute.For<IReadModels>();
        readModels
            .GetInstanceById(typeof(AccountBalanceReadModel), _eventSourceId, default)
            .Returns(Task.FromResult<object>(new AccountBalanceReadModel(0m)));

        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Replace(
            _scenario.Services,
            new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(IReadModels), readModels));
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Replace(
            _scenario.Services,
            new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(AccountBalanceReadModel), _ => new AccountBalanceReadModel(0m), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped));
    }

    async Task Because() => _result = await _scenario.Execute(new WithdrawFunds(_eventSourceId));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_validation_errors() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_validation_error_from_validator() => _result.ValidationResults.First().Message.ShouldEqual("Account has insufficient funds.");
    [Fact] void should_not_have_appended_events() => _scenario.AppendedEvents.ShouldBeEmpty();
}
