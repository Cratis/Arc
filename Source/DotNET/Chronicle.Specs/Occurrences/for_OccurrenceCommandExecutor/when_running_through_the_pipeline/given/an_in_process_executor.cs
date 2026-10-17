// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
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
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(EffectHappened)],
            ModelBoundProjections = [typeof(EffectState)]
        };
        _store = new EventStoreForTesting(null, artifacts, new ChronicleOptions(), new ScenarioTime(), capabilities);
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
        services.AddSingleton<IDecisionReads>(new CommandDecisionReads(_store.GetDecisionReads(), _store, _store.ReadModels));
        services.AddSingleton<ICommandProtectedDecisionSupport>(new DecisionDependencySafety());

        // Chronicle's raw testing log has no ambient manager; bind the transactional adapter to the command's unit.
        services.AddSingleton<ITransactionalEventSequence>(new TransactionalEventSequence(_store.EventLog, _store.UnitOfWorkManager));
        services.AddSingleton(_counter);
        services.AddSingleton<ICausationManager>(new CausationManager());
        services.AddSingleton<IOccurrenceCommandExecutor, OccurrenceCommandExecutor>();
        services.AddSingleton<IInstancesOf<ICommandExecutionScope>>(new KnownInstancesOf<ICommandExecutionScope>([new CommandCausationScope(), new TransactionalCommandScope()]));
        services.AddSingleton<IInstancesOf<ICommandFilter>>(new KnownInstancesOf<ICommandFilter>([new DataAnnotationValidationFilter()]));
        services.AddSingleton<IInstancesOf<ICommandExceptionMapper>>(new KnownInstancesOf<ICommandExceptionMapper>([new OccurrenceCommandExceptionMapper()]));
        services.AddSingleton<IInstancesOf<ICommandResponseValueHandler>>(new KnownInstancesOf<ICommandResponseValueHandler>([]));
        services.AddSingleton<IInstancesOf<ICommandContextValuesProvider>>(new KnownInstancesOf<ICommandContextValuesProvider>([]));
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(RecordEffect), typeof(IgnoreNestedFailure), typeof(CommitTooEarly), typeof(CompleteAStream), typeof(ReadEffect)]);
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
        public CorrelationId CorrelationId { get; set; } = CorrelationId.NotSet;
        public Func<Task> AfterRead { get; set; } = () => Task.CompletedTask;
        public DecisionRead<EffectState>? Read { get; set; }
        public IUnitOfWork? Unit { get; set; }
        public CommandResult? NestedResult { get; set; }
        public bool OuterHandlerCompleted { get; set; }
    }

    [Command]
    public record RecordEffect([property: Required] string Value, bool Immediate = false)
    {
        public async Task<string> Handle(IEventLog log, ITransactionalEventSequence transactional, InvocationCounter counter, ICorrelationIdAccessor correlation)
        {
            counter.Calls++;
            counter.CorrelationId = correlation.Current;
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
    public record IgnoreNestedFailure
    {
        public async Task Handle(ICommandPipeline pipeline, ITransactionalEventSequence transactional, InvocationCounter counter, IUnitOfWorkManager manager)
        {
            counter.Unit = manager.Current;
            await transactional.Append("effect-source", new EffectHappened("before-nested-command"));
            counter.NestedResult = await pipeline.Execute(new RecordEffect("must-not-commit", Immediate: true));
            await transactional.Append("effect-source", new EffectHappened("after-nested-command"));
            counter.OuterHandlerCompleted = true;
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

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record ReadEffect(bool Append)
    {
        public async Task Handle(IDecisionReads reads, ITransactionalEventSequence transactional, InvocationCounter counter, IUnitOfWorkManager manager)
        {
            counter.Calls++;
            counter.Unit = manager.Current;
            counter.Read = await reads.Get<EffectState>((ReadModelKey)"read-source");
            await counter.AfterRead();
            if (Append)
            {
                await transactional.Append("effect-source", new EffectHappened("protected"));
            }
        }
    }

    [ReadModel, FromEvent<EffectHappened>]
    public record EffectState([property: Cratis.Chronicle.Keys.Key] EventSourceId Id, string Value);

    [EventType]
    public record EffectHappened(string Value);
}
