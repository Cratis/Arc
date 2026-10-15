// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;

public class a_read_protection_pipeline : Specification
{
    protected TransactionalCommandScope _scope;
    protected UnitOfWork _unit;
    protected IEventStore _store;
    protected CommandPipeline _pipeline;
    protected IServiceProvider _services;
    protected ICommandFilters _filters;
    protected Func<CommandContext, Task<object?>> _handle;
    protected CommandResult _result;
    protected CommandContext _context;
    ActivitySource _activity;

    void Establish()
    {
        (_store, _, _unit) = DecisionFixtures.Transaction();
        _scope = new();
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(Arg.Any<CorrelationId>()).Returns(_unit);
        _services = Substitute.For<IServiceProvider>();
        _services.GetService(typeof(IUnitOfWorkManager)).Returns(manager);
        _services.GetService(typeof(IEventStore)).Returns(_store);
        _services.GetService(typeof(ICommandProtectedDecisionSupport)).Returns(Substitute.For<CommandDecisionSupportForSpecs>());
        _services.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])));
        _filters = Substitute.For<ICommandFilters>();
        _filters.OnExecution(Arg.Any<CommandContext>()).Returns(call => CommandResult.Success(call.Arg<CommandContext>().CorrelationId));
        var handler = Substitute.For<ICommandHandler>();
        _handle = _ => Task.FromResult<object?>(null);
        handler.Handle(Arg.Any<CommandContext>()).Returns(call =>
        {
            _context = call.Arg<CommandContext>();
            return new ValueTask<object?>(_handle(_context));
        });
        var providers = Substitute.For<ICommandHandlerProviders>();
        providers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = handler;
            return true;
        });
        var correlation = Substitute.For<ICorrelationIdAccessor>();
        correlation.Current.Returns(_unit.CorrelationId);
        var builder = Substitute.For<ICommandContextValuesBuilder>();
        builder.Build(Arg.Any<object>()).Returns(_ => new CommandContextValues());
        var resolver = Substitute.For<ICommandHandlerArgumentResolver>();
        resolver.Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call => new ValueTask<CommandHandlerArgumentResolution>(new CommandHandlerArgumentResolution([], CommandResult.Success(call.Arg<CommandContext>().CorrelationId))));
        _activity = new("Arc.ReadProtection.Specs");
        var activity = Substitute.For<IActivitySource<CommandPipeline>>();
        activity.ActualSource.Returns(_activity);
        _pipeline = new(
            correlation,
            _filters,
            providers,
            Substitute.For<ICommandResponseValueHandlers>(),
            Substitute.For<ICommandContextModifier>(),
            builder,
            resolver,
            new KnownInstancesOf<ICommandExecutionScope>([_scope, new ReadProtectionMeasureScope()]),
            Substitute.For<IServiceScopeFactory>(),
            activity);
    }

    protected Task<CommandResult> Execute(object? command = null, CancellationToken cancellationToken = default) =>
        _pipeline.Execute(command ?? new EnforcingCommand(), _services, null, cancellationToken);

    protected static Exception? ProbeFrame()
    {
        var (_, _, probe) = DecisionFixtures.Transaction();
        return Catch.Exception(() =>
        {
            using var frame = probe.EnforceReadProtection();
        });
    }

    void Destroy()
    {
        _activity.Dispose();
        CommandTransaction.Current = null;
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record EnforcingCommand;
    public record OffCommand;
}
