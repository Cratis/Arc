// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline.given;

public class an_in_process_executor : Specification
{
    protected ServiceProvider _services;
    protected EventStoreForTesting _store;
    protected IOccurrenceCommandExecutor _executor;
    protected OccurrenceContext _occurrence;
    protected EffectKey _key;
    protected InvocationCounter _counter;
    protected OccurrenceCommandResult _result;
    protected EffectReceipt? _receiptAtAppend;
    IDisposable _subscription;

    void Establish()
    {
        var capabilities = new KernelCapabilitiesForTesting();
        capabilities.Current();
        _store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(EffectHappened)] }, new ChronicleOptions(), new ScenarioTime(), capabilities);
        _occurrence = new OccurrenceContext("pipeline-occurrence", "pipeline-trigger", OccurrenceKind.Schedule, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new ScheduleRevision(1), 1, new LeaseToken("pipeline-occurrence", 1)) { Namespace = _store.Namespace };
        _key = _occurrence.EffectKey("record-effect");
        _counter = new();
        _subscription = _store.EventLog.AppendOperations.Subscribe(_ => _receiptAtAppend = _store.Occurrences.GetEffectReceipt(_key).GetAwaiter().GetResult());

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddSingleton<IEventStore>(_store);
        services.AddSingleton(_store.EventLog);
        services.AddSingleton(_store.UnitOfWorkManager);

        // Chronicle's raw testing log has no ambient manager; bind the transactional adapter to the command's unit.
        services.AddSingleton<ITransactionalEventSequence>(new TransactionalEventSequence(_store.EventLog, _store.UnitOfWorkManager));
        services.AddSingleton(_counter);
        services.AddSingleton<ICorrelationIdModifier>(provider => (ICorrelationIdModifier)provider.GetRequiredService<ICorrelationIdAccessor>());
        services.AddSingleton<ICausationManager>(new CausationManager());
        services.AddSingleton<IOccurrenceCommandExecutor, OccurrenceCommandExecutor>();
        services.AddSingleton<IInstancesOf<ICommandExecutionScope>>(new KnownInstancesOf<ICommandExecutionScope>([new CommandCausationScope(), new TransactionalCommandScope()]));
        services.AddSingleton<IInstancesOf<ICommandFilter>>(new KnownInstancesOf<ICommandFilter>([new DataAnnotationValidationFilter()]));
        services.AddSingleton<IInstancesOf<ICommandResponseValueHandler>>(new KnownInstancesOf<ICommandResponseValueHandler>([]));
        services.AddSingleton<IInstancesOf<ICommandContextValuesProvider>>(new KnownInstancesOf<ICommandContextValuesProvider>([]));
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(RecordEffect), typeof(CommitTooEarly), typeof(CompleteAStream)]);
        services.AddSingleton<ICommandHandlerProviders>(new CommandHandlerProviders(new KnownInstancesOf<ICommandHandlerProvider>([new CommandHandlerProvider(types)])));
        _services = services.BuildServiceProvider();
        _executor = _services.GetRequiredService<IOccurrenceCommandExecutor>();
    }

    protected Task<OccurrenceCommandResult> Execute(object command) => _executor.Execute(command, _occurrence, "record-effect");

    async Task Destroy()
    {
        _subscription.Dispose();
        await _services.DisposeAsync();
        _store.Dispose();
        CommandTransaction.Current = null;
    }

    public class InvocationCounter
    {
        public int Calls { get; set; }
    }

    [Command]
    public record RecordEffect([property: Required] string Value, bool Immediate = false)
    {
        public async Task<string> Handle(IEventLog log, ITransactionalEventSequence transactional, InvocationCounter counter, ICorrelationIdAccessor correlation)
        {
            counter.Calls++;
            if (Immediate)
            {
                await log.Append("effect-source", new EffectHappened(Value), correlationId: correlation.Current);
            }
            else
            {
                await transactional.Append("effect-source", new EffectHappened(Value));
            }

            return $"private-response:{Value}";
        }
    }

    [Command]
    public record CommitTooEarly
    {
        public async Task Handle(IUnitOfWorkManager manager)
        {
            await TransactionalCommandScope.CommitAggregate(manager.Current);
        }
    }

    [Command]
    public record CompleteAStream
    {
        public void Handle() => OccurrenceExecution.RefuseStreamCompletion();
    }

    [EventType]
    public record EffectHappened(string Value);
}
