// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_handler_takes_read_model_dependency;

public class and_command_is_executed : Specification
{
    CommandScenario<UseReadModelDependencyCommand> _scenario;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<UseReadModelDependencyCommand>();

        var readModels = Substitute.For<IReadModels>();
        readModels
            .GetInstanceById(typeof(AccountBalanceReadModel), _eventSourceId, default)
            .Returns(Task.FromResult<object>(new AccountBalanceReadModel(42m)));

        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Replace(
            _scenario.Services,
            new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(IReadModels), readModels));
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Replace(
            _scenario.Services,
            new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(AccountBalanceReadModel), _ => new AccountBalanceReadModel(42m), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped));
    }

    async Task Because() => await _scenario.Execute(new UseReadModelDependencyCommand(_eventSourceId));

    [Fact]
    async Task should_append_event_with_value_from_read_model() =>
        await _scenario.ShouldHaveAppendedEvent<UseReadModelDependencyCommand, ReadModelDependencyUsed>(
            _eventSourceId,
            @event => @event.Balance == 42m);
}
