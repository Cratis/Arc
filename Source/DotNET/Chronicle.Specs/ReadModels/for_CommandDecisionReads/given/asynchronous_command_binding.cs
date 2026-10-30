// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.given;

public class asynchronous_command_binding : protected_plain_models
{
    System.Diagnostics.ActivitySource _activity;

    protected Task<CommandResult> Execute(object command)
    {
        var correlation = Substitute.For<ICorrelationIdAccessor>();
        correlation.Current.Returns(_context.CorrelationId);
        var filters = Substitute.For<ICommandFilters>();
        filters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Success(_context.CorrelationId));
        var handler = new ModelBoundCommandHandler(command.GetType(), command.GetType().GetMethod("Handle")!);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.TryGetHandlerFor(command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = handler;
            return true;
        });
        var modifier = Substitute.For<ICommandContextModifier>();
        modifier.When(_ => _.SetCurrent(Arg.Any<CommandContext>())).Do(call => _context = call.Arg<CommandContext>());
        var values = Substitute.For<ICommandContextValuesBuilder>();
        values.Build(command).Returns(_context.Values);
        IDisposable? protection = null;
        var scope = Substitute.For<ICommandExecutionScope>();
        scope.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ =>
        {
            CommandTransaction.Current = _unit;
            protection = _unit.EnforceReadProtection();
            CommandDecisionReads.Begin(command.GetType());
        });
        scope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(_ =>
        {
            CommandDecisionReads.End();
            protection?.Dispose();
            CommandTransaction.Current = null;
            return Task.CompletedTask;
        });
        _activity = new("Cratis.Arc.AsyncBinding.Specs");
        var activity = Substitute.For<IActivitySource<CommandPipeline>>();
        activity.ActualSource.Returns(_activity);
        var pipeline = new CommandPipeline(correlation, filters, handlers, Substitute.For<ICommandResponseValueHandlers>(), modifier, values,
            new CommandHandlerArgumentResolver(new CommandProvideInvoker()), new KnownInstancesOf<ICommandExecutionScope>([scope]),
            Substitute.For<IServiceScopeFactory>(), activity);
        return pipeline.Execute(command, new InvocationServices(_services));
    }

    void Destroy() => _activity?.Dispose();

    sealed class InvocationServices(IServiceProvider inner) : IServiceProvider
    {
        readonly AuthorizationDeclarations _declarations = new(new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([]));
        public object? GetService(Type serviceType) => serviceType == typeof(AuthorizationDeclarations) ? _declarations : inner.GetService(serviceType);
    }
}
