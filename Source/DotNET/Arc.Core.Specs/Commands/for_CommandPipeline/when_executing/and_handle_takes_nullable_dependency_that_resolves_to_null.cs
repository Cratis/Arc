// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handle_takes_nullable_dependency_that_resolves_to_null : given.a_command_pipeline
{
    class Dependency;

    class Command
    {
        public bool DependencyWasNull { get; private set; }

        public void Handle(Dependency? dependency) => DependencyWasNull = dependency is null;
    }

    Command _command;
    CommandResult _result;
    ServiceProvider _provider;

    void Establish()
    {
        _command = new();
        _provider = new ServiceCollection()
            .AddScoped<Dependency>(_ => null!)
            .BuildServiceProvider();

        var handler = new ModelBoundCommandHandler(typeof(Command), typeof(Command).GetMethod(nameof(Command.Handle))!);
        _commandHandlerProviders
            .TryGetHandlerFor(_command, out var anyHandler)
            .Returns(call =>
            {
                call[1] = handler;
                return true;
            });

        var activitySource = Substitute.For<IActivitySource<CommandPipeline>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activitySource.ActualSource.Returns(_activitySource);

        _commandPipeline = new(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder,
            new CommandHandlerArgumentResolver(new CommandProvideInvoker()),
            _serviceScopeFactory,
            activitySource);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _provider);

    void Destroy() => _provider.Dispose();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_inject_null_into_handle() => _command.DependencyWasNull.ShouldBeTrue();
}
