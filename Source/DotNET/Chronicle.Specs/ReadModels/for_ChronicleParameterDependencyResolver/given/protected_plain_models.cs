// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Queries;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

public class protected_plain_models : Specification
{
    protected IDecisionReads _inner;
    protected IReadModels _stored;
    protected UnitOfWork _unit;
    protected ServiceProvider _services;
    protected List<DecisionRead<Balance>> _tokens;
    protected CommandContext _context;
    protected Balance _from;
    protected Balance _to;
    protected ProtectedReadModelKeyOptions _keyOptions;
    ChronicleParameterDependencyResolver _resolver;

    void Establish()
    {
        var (store, _, unit) = for_CommandDecisionReads.DecisionFixtures.Transaction();
        _unit = unit;
        _inner = Substitute.For<for_CommandDecisionReads.when_reading_a_stream.given.DecisionReaderForSpecs>();
        _stored = Substitute.For<IReadModels>();
        _tokens = [];
        _keyOptions = new();
        _from = new(new("from"));
        _to = new(new("to"));
        var type = new Cratis.Chronicle.Events.EventType((Cratis.Chronicle.Events.EventTypeId)Guid.NewGuid().ToString(), Cratis.Chronicle.Events.EventTypeGeneration.First);
        _inner.Describe<Balance>().Returns(new DecisionReadDescription(new(true, null), [type], false));
        foreach (var model in new[] { _from, _to })
        {
            var token = for_CommandDecisionReads.DecisionFixtures.Protected(model.Id.Value, model);
            _inner.GetFor<Balance>(_unit, (ReadModelKey)model.Id.Value, Arg.Any<CancellationToken>()).Returns(_ =>
            {
                _unit.AddDecisionRead(token);
                _tokens.Add(token);
                return token;
            });
            _inner.GetDetached<Balance>((ReadModelKey)model.Id.Value, Arg.Any<CancellationToken>()).Returns(token);
            _stored.Release(model).Returns(model);
            _stored.GetInstanceById(typeof(Balance), (ReadModelKey)model.Id.Value, default).Returns(model);
        }
        var accessor = Substitute.For<ICommandContextAccessor>();
        _context = new(Cratis.Execution.CorrelationId.New(), typeof(Transfer), new Transfer(new("from"), new("to")), [], new()
        {
            [WellKnownCommandContextKeys.EventSourceId] = (Cratis.Chronicle.Events.EventSourceId)"from"
        });
        accessor.Current.Returns(_ => _context);
        ReadModelDispatch.Register<Balance>();
        _resolver = new([typeof(Balance)]);
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .AddSingleton<ICommandProtectedDecisionSupport>(new DecisionDependencySafety())
            .AddSingleton(accessor)
            .AddSingleton(_ => _context)
            .AddSingleton(new RegisteredReadModelTypes([typeof(Balance)]))
            .AddSingleton<IInstancesOf<IUnresolvableDependencyClassifier>>(new KnownInstancesOf<IUnresolvableDependencyClassifier>([new ReadModelUnresolvableDependencyClassifier()]))
            .AddSingleton(_stored)
            .AddProtectedKeyTraversal(_keyOptions)
            .AddSingleton<IDecisionReads>(new CommandDecisionReads(_inner, store, _stored))
            .BuildServiceProvider();
    }

    protected object? Resolve(ParameterInfo parameter)
    {
        _resolver.TryResolve(_services, parameter, out var value).ShouldBeTrue();
        return value;
    }

    protected static IReadOnlyList<ParameterInfo> HandleParameters => typeof(Transfer).GetMethod(nameof(Transfer.Handle))!.GetParameters();

    void Destroy()
    {
        _services.Dispose();
        CommandTransaction.Current = null;
    }

    public record AccountId(string Value) : Cratis.Concepts.ConceptAs<string>(Value);
    public record Balance(AccountId Id);
    [ReadProtection(ReadProtectionMode.On)]
    public record Transfer(AccountId From, AccountId To)
    {
        public void Handle([LookUpBy(nameof(From))] Balance from, [LookUpBy(nameof(To))] Balance to) { }
        [Unprotected("only the body is advisory")]
        public Balance Provide([LookUpBy(nameof(From))] Balance balance) => balance;
        public void FromDatabase(global::MongoDB.Driver.IMongoCollection<Balance> collection) { }
        public void Enrich([LookUpBy(nameof(From)), Unprotected("context only")] Balance context, [LookUpBy(nameof(From))] Balance balance) { }
    }

    public class Validator([LookUpBy(nameof(Transfer.From))] Balance balance) : CommandValidator<Transfer>
    {
        public Balance Balance { get; } = balance;
    }
}
