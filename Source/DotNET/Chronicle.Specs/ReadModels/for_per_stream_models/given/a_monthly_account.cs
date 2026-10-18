// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models.given;

public class a_monthly_account : Specification
{
    protected EventStoreForTesting _store;
    protected ServiceProvider _services;
    protected CommandContext _context;
    protected UnitOfWork _unit;
    protected TransactionalCommandScope _scope;
    protected readonly AccountId _account = new("account");
    ChronicleParameterDependencyResolver _resolver;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(MoneyWithdrawn)],
            ModelBoundProjections = [typeof(MonthlyWithdrawals), typeof(AccountWithdrawals)],
            EventSources = [typeof(BankAccount)]
        };
        _store = new EventStoreForTesting(null, artifacts);
        _resolver = new([typeof(MonthlyWithdrawals), typeof(AccountWithdrawals)]);
        var accessor = Substitute.For<ICommandContextAccessor>();
        accessor.Current.Returns(_ => _context);
        _services = new ServiceCollection()
            .AddSingleton(accessor)
            .AddTransient(_ => _context)
            .AddSingleton(_store.EventLog)
            .AddSingleton<IEventStore>(_store)
            .AddSingleton(_store.EventSources)
            .AddSingleton(_store.UnitOfWorkManager)
            .AddSingleton(_store.ReadModels)
            .AddSingleton<ICommandProtectedDecisionSupport>(new DecisionDependencySafety())
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .AddReadModels(artifacts)
            .BuildServiceProvider();
        _scope = new();
    }

    protected void Begin(object command, string? stream = "Transactions", string? sourceType = "BankAccount", bool beginScope = true)
    {
        var values = new CommandContextValues { [WellKnownCommandContextKeys.EventSourceId] = _account };
        if (stream is not null) values[WellKnownCommandContextKeys.EventStreamType] = (EventStreamType)stream;
        if (sourceType is not null) values[WellKnownCommandContextKeys.EventSourceType] = (EventSourceType)sourceType;
        _context = new(Cratis.Execution.CorrelationId.New(), command.GetType(), command, [], values, ServiceProvider: _services);
        if (beginScope)
        {
            _scope.Begin(_context);
            _unit = (UnitOfWork)CommandTransaction.Current!;
        }
    }

    protected object? Resolve(string method = nameof(Withdraw.Handle), int parameter = 0) => Resolve(typeof(Withdraw).GetMethod(method)!.GetParameters()[parameter]);

    protected object? Resolve(ParameterInfo parameter)
    {
        _resolver.TryResolve(_services, parameter, out var model).ShouldBeTrue();
        return model;
    }

    protected WithdrawValidator Validate()
    {
        var types = Substitute.For<Cratis.Types.ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(WithdrawValidator)]);
        new DiscoverableValidators(types).TryGet(typeof(Withdraw), _services, out var validator).ShouldBeTrue();
        return (WithdrawValidator)validator!;
    }

    protected async Task Append(string month, decimal amount)
    {
        var route = new EventRoutes(_store.EventSources).For<BankAccount>("Transactions").WithStreamId(month);
        (await _store.EventLog.AppendMany([route.Route(_account, new MoneyWithdrawn(amount))])).IsSuccess.ShouldBeTrue();
    }

    protected async Task Compete(string month)
    {
        Task append;
        using (ExecutionContext.SuppressFlow()) append = Task.Run(() => Append(month, 9000));
        await append;
    }

    protected async Task<CommandResult> Complete()
    {
        _store.EventLog.TryEnrollForCommand(_account, new MoneyWithdrawn(100), _context, null).ShouldBeTrue();
        var result = CommandResult.Success(_context.CorrelationId);
        await _scope.Complete(_context, result);
        Assert.False(result.HasExceptions, string.Join(Environment.NewLine, result.ExceptionMessages));
        return result;
    }

    void Destroy()
    {
        if (_context is not null) _scope.End(_context);
        _services.Dispose();
        _store.Dispose();
        CommandTransaction.Current = null;
    }

    [Cratis.Chronicle.EventSources.EventSource("BankAccount")]
    [Cratis.Chronicle.EventSources.EventStream("Transactions")]
    public class BankAccount : IEventSource;
    public record AccountId(string Value) : EventSourceId(Value);
    [EventType] public record MoneyWithdrawn(decimal Amount);
    [ReadModel, EventStream<BankAccount>("Transactions")]
    public record MonthlyWithdrawals(AccountId Id, [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Total)
    {
        public static async Task<MonthlyWithdrawals?> ForMonth(AccountId account, string month, IReadModels models)
        {
            var instance = await models.GetInstanceById<MonthlyWithdrawals>(account, (EventStreamId)month);
            return instance is null ? null : await models.Release(instance);
        }
    }
    [ReadModel]
    public record AccountWithdrawals(AccountId Id, [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Total);

    [Command, ReadProtection(ReadProtectionMode.On), EventSource<BankAccount>("Transactions"), EventStreamId("{Month}")]
    public record Withdraw(AccountId Account, string Month, decimal Amount)
    {
        public MoneyWithdrawn Handle(MonthlyWithdrawals? month) => new(Amount);
        public MoneyWithdrawn Required(MonthlyWithdrawals month) => new(Amount);
        public MoneyWithdrawn Mix(MonthlyWithdrawals? month, AccountWithdrawals? total) => new(Amount);
        public MoneyWithdrawn Redirect([LookUpBy(nameof(Account))] MonthlyWithdrawals? month) => new(Amount);
        public MoneyWithdrawn Advisory([Unprotected("display only")] MonthlyWithdrawals? month) => new(Amount);
    }

    [ReadProtection(ReadProtectionMode.Off), EventSource<BankAccount>("Transactions"), EventStreamId("{Month}")]
    public record Off(AccountId Account, string Month, decimal Amount) : Withdraw(Account, Month, Amount);
    [ReadProtection(ReadProtectionMode.Measure), EventSource<BankAccount>("Transactions"), EventStreamId("{Month}")]
    public record Measure(AccountId Account, string Month, decimal Amount) : Withdraw(Account, Month, Amount);
    [ReadProtection(ReadProtectionMode.On)]
    public record NoStreamId(AccountId Account);
    [ProtectedDecision, EventSource<BankAccount>("Transactions"), EventStreamId("{Month}")]
    public record ExplicitDecision(AccountId Account, string Month)
    {
        public MoneyWithdrawn Handle(DecisionRead<MonthlyWithdrawals> month) => new(100);
    }

    public class WithdrawValidator : CommandValidator<Withdraw>
    {
        public WithdrawValidator(MonthlyWithdrawals? month)
        {
            Month = month;
            RuleFor(command => command.Amount).Must(amount => (month?.Total ?? 0) + amount <= 10_000).WithMessage("Monthly limit reached");
        }
        public MonthlyWithdrawals? Month { get; }
    }
}
