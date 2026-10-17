// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Occurrences;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_executing_for_an_occurrence.given;

public class an_occurrence_execution : Specification
{
    protected TransactionalCommandScope _scope;
    protected EventStoreForTesting _store;
    protected IServiceProvider _serviceProvider;
    protected CommandContext _context;
    protected CorrelationId _correlationId;
    protected EffectKey _key;
    internal OccurrenceExecution _execution;
    protected IDisposable _executionScope;
    protected CommandResult _result;
    protected Exception _exception;
    protected IUnitOfWork _unit;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        var capabilities = new KernelCapabilitiesForTesting();
        capabilities.Current();
        _store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(ScopedEffectHappened)] }, new ChronicleOptions(), new ScenarioTime(), capabilities);
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkManager>(_store.UnitOfWorkManager);
        services.AddSingleton<IEventLog>(_store.EventLog);
        _serviceProvider = services.BuildServiceProvider();
        _key = new EffectKey("occurrence-1", "effect", new LeaseToken("occurrence-1", 1));
        _execution = new OccurrenceExecution(_key, nameof(SomeCommand));
        _scope = new();
        _context = new(_correlationId, typeof(SomeCommand), new SomeCommand(), [], new(), ServiceProvider: _serviceProvider);
    }

    protected void BeginExecution() => _executionScope = OccurrenceExecution.Begin(_execution);

    void Destroy() => _executionScope?.Dispose();

    protected static void AddEvent() =>
        CommandTransaction.Current!.AddEvent(EventSequenceId.Log, "source", new ScopedEffectHappened(1), Cratis.Chronicle.Auditing.Causation.Unknown());

    protected class SomeCommand;

    [EventType]
    protected record ScopedEffectHappened(int Number);
}
