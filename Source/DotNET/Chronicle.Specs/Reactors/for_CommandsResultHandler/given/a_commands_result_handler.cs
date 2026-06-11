// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.Reactors;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandsResultHandler.given;

public class a_commands_result_handler : Specification
{
    protected CommandsResultHandler _handler;
    protected IServiceScopeFactory _serviceScopeFactory;
    protected IServiceScope _serviceScope;
    protected IServiceProvider _serviceProvider;
    protected ICommandPipeline _commandPipeline;
    protected ReactorContext _reactorContext = null!;
    protected IEventStore _eventStore = null!;

    void Establish()
    {
        _commandPipeline = Substitute.For<ICommandPipeline>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceScope = Substitute.For<IServiceScope>();
        _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

        _serviceScope.ServiceProvider.Returns(_serviceProvider);
        _serviceScopeFactory.CreateScope().Returns(_serviceScope);
        _serviceProvider.GetService(typeof(ICommandPipeline)).Returns(_commandPipeline);

        _handler = new CommandsResultHandler(_serviceScopeFactory);
    }

    [Command]
    public record TestCommand(string Name)
    {
        public Task Handle() => Task.CompletedTask;
    }

    public class NotACommand
    {
        public string Name { get; set; } = string.Empty;
    }
}
