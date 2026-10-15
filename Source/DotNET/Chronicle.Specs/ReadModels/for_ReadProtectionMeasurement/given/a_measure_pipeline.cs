// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement.given;

public class a_measure_pipeline : a_read_protection_pipeline
{
    protected IEventLog _log;
    protected DescriptionReader _reader;
    protected Subject<IEnumerable<AppendedEventWithResult>> _appends;
    protected readonly ConcurrentQueue<(string Category, string? Reason)> _telemetry = new();
    protected readonly List<string> _batches = [];
    protected readonly HashSet<string> _additionalCommands = [];
    protected readonly EventType _eventType = new("fact", EventTypeGeneration.First);
    protected readonly Stock _stock = new("subject");
    protected IConfiguration _configuration;
    MeterListener _listener;

    protected Type MeasuredCommandType => typeof(MeasureCommand<>).MakeGenericType(GetType());

    void Establish()
    {
        _log = Substitute.For<MeasuredEventLog>();
        _store.GetEventSequence(EventSequenceId.Log).Returns(_log);
        _log.Id.Returns(EventSequenceId.Log);
        _services.GetService(typeof(IEventLog)).Returns(_log);
        _reader = new(_eventType);
        typeof(DecisionReads).GetMethod("RegisterTesting", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [_store, _reader]);
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReadProtection:MeasureSampleRate"] = "0"
        }).Build();
        _services.GetService(typeof(IConfiguration)).Returns(_ => _configuration);
        _appends = new();
        _log.AppendOperations.Returns(_appends);
        _log.GetTailSequenceNumber().Returns((EventSequenceNumber)100ul);
        _log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
                var scopes = call.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
                _batches.Add(string.Join(';', events.Select(_ => $"{_.EventSourceId}:{_.Event}")) + "|" + string.Join(';', scopes.Select(_ => $"{_.Key}:{_.Value}")));
                _appends.OnNext(events.Select(_ =>
                {
                    var context = EventContext.Empty with
                    {
                        EventSourceId = _.EventSourceId,
                        EventType = _eventType,
                        SequenceNumber = 102ul,
                        CorrelationId = _unit.CorrelationId
                    };
                    return new AppendedEventWithResult(new(context, _.Event), AppendResult.Success(_unit.CorrelationId, 102ul));
                }).ToArray());
                return AppendManyResult.Success(_unit.CorrelationId, [(EventSequenceNumber)102ul]);
            });
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Cratis.Arc.ReadProtection") listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string? command = null;
            string? category = null;
            string? reason = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "command") command = tag.Value as string;
                if (tag.Key == "category") category = tag.Value as string;
                if (tag.Key == "reason") reason = tag.Value as string;
            }
            if (category is not null && (command == MeasuredCommandType.FullName || command == GetType().FullName || (command is not null && _additionalCommands.Contains(command)))) _telemetry.Enqueue((category, reason));
        });
        _listener.Start();
    }

    protected async Task<CommandResult> Run(bool measure, Func<CommandContext, Task> action, object? command = null)
    {
        _configuration["ReadProtection:MaximumMode"] = measure ? null : "Off";
        var unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        _unit = unit;
        var manager = (IUnitOfWorkManager)_services.GetService(typeof(IUnitOfWorkManager))!;
        manager.Begin(Arg.Any<CorrelationId>()).Returns(unit);
        _handle = async context =>
        {
            context.Values[WellKnownCommandContextKeys.EventSourceId] = (EventSourceId)"subject";
            await action(context);
            return null;
        };
        return await Execute(command ?? Activator.CreateInstance(MeasuredCommandType));
    }

    protected void ReadParameter(CommandContext context, string name)
    {
        var parameter = typeof(Dependencies).GetMethod(nameof(Dependencies.Handle))!.GetParameters().Single(_ => _.Name == name);
        var resolver = new ChronicleParameterDependencyResolver([typeof(Stock), typeof(JoinedStock), typeof(AdvisoryStock)]);
        resolver.TryResolve(_services, parameter, out _);
        if (parameter.ParameterType == typeof(Stock))
        {
            var readModels = Substitute.For<IReadModels>();
            readModels.GetInstanceById(typeof(Stock), (ReadModelKey)"subject", default).Returns(Task.FromResult<object>(_stock));
            readModels.Release(_stock).Returns(_stock);
            ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(Stock), context, readModels).ShouldBeSame(_stock);
        }
    }

    protected EventMetadata Metadata(ulong position, string source = "subject", string stream = "") => new(
        position,
        _eventType.Id,
        EventSourceType.Default,
        source,
        stream.Length == 0 ? EventStreamType.All : (EventStreamType)stream,
        EventStreamId.Default,
        DateTimeOffset.UnixEpoch,
        _unit.CorrelationId,
        [],
        null!,
        default,
        [],
        new Cratis.Chronicle.Subject(source),
        EventContext.Empty.EventSource);

    protected void Stage(CommandContext context) => _log.TryEnrollForCommand("subject", new Fact("value"), context, new ConcurrencyScope(90ul, (EventSourceId)"subject"));

    void Destroy()
    {
        _listener.Dispose();
        _appends.Dispose();
    }

    [ReadProtection(ReadProtectionMode.Measure)]
    public record MeasureCommand<T>;
    public record Stock(string Id);
    public record JoinedStock(string Id);
    public record AdvisoryStock(string Id);
    [EventType]
    public record Fact(string Value);
    public interface ICapacity;

    public class Dependencies
    {
        public void Handle(Stock stock, JoinedStock joined, [Unprotected("Prompt context")] AdvisoryStock advisory, IReadModels stored, IEventLog raw, IEventStore store, IMongoCollection<Stock> mongo, ICapacity capacity) { }
    }

    public class DescriptionReader(EventType eventType) : IDecisionReads
    {
        public bool FailDescription { get; set; }
        public int Acquisitions { get; private set; }
        public DecisionReadDescription Describe<T>()
            where T : class => FailDescription ? throw new InvalidOperationException("description failure") : new(Admit<T>(), [eventType], false);
        public DecisionReadAdmission Admit<T>()
            where T : class => typeof(T) == typeof(JoinedStock) ? new(false, DecisionReadRefusalReason.Hierarchy) : new(true, null);
        public DecisionReadAdmission AdmitForStream<T>()
            where T : class => Admit<T>();
        public Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
            where T : class => GetDetached<T>(key, cancellationToken);
        public Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
            where T : class
        {
            Acquisitions++;
            return Task.FromResult(DecisionFixtures.Protected<T>(key.Value));
        }
    }
}
