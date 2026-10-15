// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs.given;

public class a_protected_read : Specification
{
    protected EventStoreForTesting _store;
    protected ServiceProvider _services;
    protected CommandContext _context;
    protected TransactionalCommandScope _scope;
    protected readonly BookId _book = new("book");
    protected readonly MemberId _member = new("member");
    protected BorrowValidator _validator;
    protected UnitOfWork _unit;
    ChronicleParameterDependencyResolver _resolver;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(StockChanged), typeof(LoanCountChanged), typeof(BookBorrowed)],
            ModelBoundProjections = [typeof(BookStock), typeof(MemberLoans)]
        };
        _store = new EventStoreForTesting(null, artifacts);
        _resolver = new ChronicleParameterDependencyResolver([typeof(BookStock), typeof(MemberLoans)]);
        var accessor = Substitute.For<ICommandContextAccessor>();
        accessor.Current.Returns(_ => _context);
        var stored = Substitute.For<IReadModels>();
        var contextOnly = new MemberLoans(_member, 2);
        stored.GetInstanceById(typeof(MemberLoans), Arg.Any<ReadModelKey>(), default).Returns(contextOnly);
        stored.Release(contextOnly).Returns(contextOnly);
        _services = new ServiceCollection()
            .AddSingleton(accessor)
            .AddSingleton<IEventStore>(_store)
            .AddSingleton(_store.UnitOfWorkManager)
            .AddSingleton(stored)
            .AddSingleton<ICommandProtectedDecisionSupport>(new DecisionDependencySafety())
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .AddSingleton<IDecisionReads>(new CommandDecisionReads(_store.GetDecisionReads(), _store, _store.ReadModels))
            .BuildServiceProvider();
        _scope = new();
    }

    protected async Task Seed(int available = 1)
    {
        (await _store.EventLog.Append(_book, new StockChanged(available))).IsSuccess.ShouldBeTrue();
        (await _store.EventLog.Append((EventSourceId)_member.Value, new LoanCountChanged(2))).IsSuccess.ShouldBeTrue();
    }

    protected void Begin(object command)
    {
        _context = new(
            Cratis.Execution.CorrelationId.New(),
            command.GetType(),
            command,
            [],
            new() { [WellKnownCommandContextKeys.EventSourceId] = _book },
            ServiceProvider: _services);
        _scope.Begin(_context);
        _unit = (UnitOfWork)CommandTransaction.Current!;
    }

    protected object? Resolve(ParameterInfo parameter)
    {
        _resolver.TryResolve(_services, parameter, out var dependency).ShouldBeTrue();
        return dependency;
    }

    protected BorrowValidator Validate(Borrow command)
    {
        var types = Substitute.For<Cratis.Types.ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(BorrowValidator)]);
        new DiscoverableValidators(types).TryGet(typeof(Borrow), _services, out var validator).ShouldBeTrue();
        _validator = (BorrowValidator)validator!;
        return _validator;
    }

    protected async Task Compete()
    {
        Task append;
        using (ExecutionContext.SuppressFlow())
        {
            append = Task.Run(async () => (await _store.EventLog.Append(_book, new StockChanged(0))).IsSuccess.ShouldBeTrue());
        }
        await append;
    }

    protected void Stage() => _store.EventLog.TryEnrollForCommand(_book, new BookBorrowed(_member), _context, null).ShouldBeTrue();

    protected async Task<CommandResult> Complete()
    {
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

    public record BookId(string Value) : EventSourceId(Value);
    public record MemberId(string Value) : Cratis.Concepts.ConceptAs<string>(Value);
    [EventType] public record StockChanged(int Available);
    [EventType] public record LoanCountChanged(int Count);
    [EventType] public record BookBorrowed(MemberId Member);
    [ReadModel, FromEvent<StockChanged>]
    public record BookStock([property: Key] BookId Id, int Available);
    [ReadModel, FromEvent<LoanCountChanged>]
    public record MemberLoans([property: Key] MemberId Id, int Count);

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record Borrow(BookId Id, MemberId Member)
    {
        public BookBorrowed Handle(BookStock stock) => new(Member);
        public BookBorrowed WithMember(BookStock stock, MemberLoans loans) => new(Member);
        public BookBorrowed Enrich([Unprotected("prompt context only")] MemberLoans context, BookStock stock) => new(Member);
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record Move(BookId Id, BookId From, BookId To)
    {
        public BookBorrowed Handle([LookUpBy(nameof(From))] BookStock from, [LookUpBy(nameof(To))] BookStock to) => new(new("member"));
    }

    public class BorrowValidator : CommandValidator<Borrow>
    {
        public BorrowValidator(BookStock stock)
        {
            Stock = stock;
            RuleFor(command => command.Id).Must(_ => stock.Available > 0).WithMessage("No copies left.");
        }

        public BookStock Stock { get; }
    }
}
