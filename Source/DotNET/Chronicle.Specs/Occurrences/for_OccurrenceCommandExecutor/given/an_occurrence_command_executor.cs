// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;
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
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.given;

public class an_occurrence_command_executor : Specification
{
    protected OccurrenceCommandExecutor _executor;
    protected IServiceScopeFactory _scopeFactory;
    protected IServiceProvider _services;
    protected ISystemExecution _systemExecution;
    protected ITenantScope _tenantScope;
    protected IEventStore _eventStore;
    protected EventStoreForTesting _store;
    protected KernelCapabilitiesForTesting _capabilities = new();
    protected ICommandPipeline _pipeline;
    protected ICorrelationIdModifier _correlationIdModifier;
    protected ICausationManager _causationManager;
    protected IUnitOfWork _unitOfWork;
    protected OccurrenceContext _occurrence;
    protected OccurrenceCommandResult _result;
    protected Exception _exception;
    protected object _command;
    protected CommandResult _commandResult;
    protected Func<Task> _duringExecution;
    protected EffectKey _key;
    protected int _pipelineCalls;
    protected EventStoreNamespaceName? _occurrenceNamespace;

    protected Task<EffectReceipt?> StoredReceipt => _eventStore.Occurrences.GetEffectReceipt(_key);

    protected static EffectReceipt ReceiptFor(EffectKey key, EffectOutcome outcome) =>
        new(key, outcome, new JsonObject { ["stored"] = true }, key.WrittenBy, DateTimeOffset.UtcNow);

    void Establish()
    {
        _capabilities.Current();
        _command = new SomeCommand();
        _commandResult = CommandResult.Success(CorrelationId.New());
        _duringExecution = () => Task.CompletedTask;
        var lease = new LeaseToken("occurrence-1", 3);
        _occurrence = new OccurrenceContext("occurrence-1", "trigger-1", OccurrenceKind.Schedule, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow, new ScheduleRevision(1), 2, lease);
        _key = _occurrence.EffectKey("send-reminder");

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _correlationIdModifier = Substitute.For<ICorrelationIdModifier>();
        _causationManager = Substitute.For<ICausationManager>();
        _pipeline = Substitute.For<ICommandPipeline>();
        _pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<Cratis.Arc.Validation.ValidationResultSeverity?>()).Returns(async _ =>
        {
            _pipelineCalls++;
            await _duringExecution();
            return _commandResult;
        });

        _services = Substitute.For<IServiceProvider>();
        _services.GetService(typeof(ICorrelationIdModifier)).Returns(_correlationIdModifier);
        _services.GetService(typeof(ICausationManager)).Returns(_causationManager);
        _services.GetService(typeof(ICommandPipeline)).Returns(_pipeline);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_services);
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(scope);
        _systemExecution = Substitute.For<ISystemExecution>();
        _tenantScope = Substitute.For<ITenantScope>();
    }

    protected void CreateExecutor()
    {
        _store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(OccurrenceEffectHappened)] }, new ChronicleOptions(), new ScenarioTime(), _capabilities);
        _eventStore = _store;
        _occurrence = WithNamespace(_occurrence, _occurrenceNamespace ?? _store.Namespace);
        _key = _occurrence.EffectKey("send-reminder");
        _services.GetService(typeof(IEventStore)).Returns(_eventStore);
        _executor = new OccurrenceCommandExecutor(_scopeFactory, _systemExecution, _tenantScope, Substitute.For<ILogger<OccurrenceCommandExecutor>>());
    }

    protected static EffectReceiptResult Recorded(EffectReceipt receipt) => new(EffectReceiptStatus.Recorded, receipt);

    protected static EffectReceiptResult AlreadyRecorded(EffectReceipt receipt) => new(EffectReceiptStatus.AlreadyRecorded, receipt);

    protected static EffectOutcome AppendedOutcome => new Appended(EventSequenceId.Log, EventSequenceNumber.First, EventSequenceNumber.First);

    protected static OccurrenceContext WithNamespace(OccurrenceContext occurrence, EventStoreNamespaceName namespaceName) =>
        new(occurrence.Id, occurrence.Trigger, occurrence.Kind, occurrence.DueAt, occurrence.RecordedAt, occurrence.Revision, occurrence.Attempt, occurrence.Lease) { Namespace = namespaceName };

    protected IUnitOfWork NewUnit(int events)
    {
        var unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        for (var index = 0; index < events; index++)
        {
            unit.AddEvent(EventSequenceId.Log, $"source-{index}", new OccurrenceEffectHappened(index), Causation.Unknown());
        }

        return unit;
    }

    protected class SomeCommand;

    [EventType]
    protected record OccurrenceEffectHappened(int Number);
}
