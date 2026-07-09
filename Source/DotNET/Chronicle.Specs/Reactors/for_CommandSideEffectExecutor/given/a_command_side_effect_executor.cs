// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.given;

public class a_command_side_effect_executor : Specification
{
    protected CommandSideEffectExecutor _executor;
    protected IServiceScopeFactory _serviceScopeFactory;
    protected IServiceScope _serviceScope;
    protected IServiceProvider _serviceProvider;
    protected ICommandPipeline _commandPipeline;
    protected ISystemExecution _systemExecution;
    protected Type _reactorType = typeof(TestReactor);

    void Establish()
    {
        _commandPipeline = Substitute.For<ICommandPipeline>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceScope = Substitute.For<IServiceScope>();
        _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        _systemExecution = Substitute.For<ISystemExecution>();

        _serviceScope.ServiceProvider.Returns(_serviceProvider);
        _serviceScopeFactory.CreateScope().Returns(_serviceScope);
        _serviceProvider.GetService(typeof(ICommandPipeline)).Returns(_commandPipeline);

        _executor = new CommandSideEffectExecutor(_serviceScopeFactory, _systemExecution);
    }

    [Command]
    public record TestCommand(string Name)
    {
        public Task Handle() => Task.CompletedTask;
    }

    public class TestReactor;

    [ExecuteCommandsAsSystem("Administrator")]
    public class SystemReactor;
}
