// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_command_pipeline : Specification
{
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected ICommandFilters _commandFilters;
    protected ICommandHandlerProviders _commandHandlerProviders;
    protected ICommandResponseValueHandlers _commandResponseValueHandlers;
    protected ICommandContextModifier _commandContextModifier;
    protected ICommandContextValuesBuilder _commandContextValuesBuilder;
    protected ICommandHandlerArgumentResolver _commandHandlerArgumentResolver;
    protected IServiceProvider _serviceProvider;
    protected IServiceScopeFactory _serviceScopeFactory;
    protected IServiceScope _serviceScope;
    protected CommandPipeline _commandPipeline;
    protected CorrelationId _correlationId;
    protected System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _correlationIdAccessor.Current.Returns(_correlationId);
        _commandFilters = Substitute.For<ICommandFilters>();
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Success(_correlationId));
        _commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        _commandResponseValueHandlers = Substitute.For<ICommandResponseValueHandlers>();
        _commandContextModifier = Substitute.For<ICommandContextModifier>();
        _commandContextValuesBuilder = Substitute.For<ICommandContextValuesBuilder>();
        _commandContextValuesBuilder.Build(Arg.Any<object>()).Returns(new CommandContextValues());
        _commandHandlerArgumentResolver = Substitute.For<ICommandHandlerArgumentResolver>();
        _commandHandlerArgumentResolver
            .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(_ => new ValueTask<CommandHandlerArgumentResolution>(new CommandHandlerArgumentResolution([], CommandResult.Success(_correlationId))));
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceScope = Substitute.For<IServiceScope>();
        _serviceScope.ServiceProvider.Returns(_serviceProvider);
        _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        _serviceScopeFactory.CreateScope().Returns(_serviceScope);

        _commandPipeline = new(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder,
            _commandHandlerArgumentResolver,
            _serviceScopeFactory,
            CreateActivitySource<CommandPipeline>());
    }

    void Cleanup()
    {
        _activitySource?.Dispose();
    }

    IActivitySource<T> CreateActivitySource<T>()
    {
        var activitySource = Substitute.For<IActivitySource<T>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activitySource.ActualSource.Returns(_activitySource);
        return activitySource;
    }
}
