// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandOperationExecution.given;

public class an_operation_pipeline : for_CommandPipeline.given.a_command_pipeline
{
    protected OperationLog _log;
    protected ServiceCollection _services;
    protected CommandResult _result;
    protected ICommandExecutionScope[] _scopes = [];
    ServiceProvider _provider;

    void Establish()
    {
        _log = new();
        _services = new();
        _services.AddSingleton(_log);
        var anyCommand = Arg.Any<object>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(anyCommand, out anyHandler).Returns(call =>
        {
            var type = call[0].GetType();
            call[1] = new ModelBoundCommandHandler(type, type.GetMethod("Handle")!);
            return true;
        });
    }

    protected async Task<CommandResult> Run(object command, CancellationToken cancellationToken = default)
    {
        _provider = _services.BuildServiceProvider();
        var activitySource = Substitute.For<IActivitySource<CommandPipeline>>();
        activitySource.ActualSource.Returns(_activitySource);
        var pipeline = new CommandPipeline(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder,
            _commandHandlerArgumentResolver,
            new KnownInstancesOf<ICommandExecutionScope>(_scopes),
            _serviceScopeFactory,
            activitySource);

        return await pipeline.Execute(command, _provider, null, cancellationToken);
    }

    void Destroy() => _provider?.Dispose();
}
