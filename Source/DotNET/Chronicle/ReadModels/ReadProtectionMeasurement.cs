// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Observes the legacy invocation; it never acquires a decision read or resolves a strategy.
/// </summary>
internal sealed class ReadProtectionMeasurement
{
    static readonly AsyncLocal<ReadProtectionMeasurement?> _current = new();
    static readonly AsyncLocal<int> _committing = new();
    readonly object _gate = new();
    readonly List<MeasuredRead> _reads = [];
    readonly List<(EventSourceId Label, ConcurrencyScope Scope, string Reason)> _scopes = [];
    readonly HashSet<EventSequenceNumber> _ownPositions = [];
    readonly Dictionary<Type, string?> _advisory = [];
    readonly Dictionary<Type, bool> _explicitLookups = [];
    readonly ReadProtectionMeasurement? _previous;
    IDisposable? _subscription;
    bool _unavailable;
    bool _ended;

    ReadProtectionMeasurement(CommandContext context)
    {
        Context = context;
        _previous = _current.Value;
    }

    /// <summary>
    /// Gets the live measurement on this invocation flow.
    /// </summary>
    internal static ReadProtectionMeasurement? Current => _current.Value is { _ended: false } current ? current : null;

    /// <summary>
    /// Gets the optional structured telemetry sink.
    /// </summary>
    internal ILogger<ReadProtectionMeasurement>? Logger { get; private set; }

    CommandContext Context { get; }
    IEventLog? EventLog { get; set; }
    IDecisionReads? DecisionReads { get; set; }
    ReadProtectionMeasureQueue? Queue { get; set; }
    EventSequenceNumber? Boundary { get; set; }
    TimeSpan TailTimeout { get; set; } = TimeSpan.FromMilliseconds(250);
    TimeSpan ScanTimeout { get; set; } = TimeSpan.FromSeconds(5);
    int ScanLimit { get; set; } = 500;
    double SampleRate { get; set; } = 0.1;

    /// <summary>
    /// Establishes an observational context synchronously so it flows through filters and providers.
    /// </summary>
    /// <param name="context">The actual invocation.</param>
    /// <returns>The measurement, or null outside Measure.</returns>
    internal static ReadProtectionMeasurement? Begin(CommandContext context)
    {
        if (CommandDecisionPolicy.ReadProtection != ReadProtectionMode.Measure) return null;
        var measurement = new ReadProtectionMeasurement(context);
        _current.Value = measurement;
        measurement.Try(() => measurement.Initialize(context.ServiceProvider));
        return measurement;
    }

    /// <summary>
    /// Ends only the matching invocation, tolerating scopes whose Begin did not run.
    /// </summary>
    /// <param name="context">The exiting command.</param>
    internal static void EndInvocation(CommandContext context)
    {
        if (Current is { } measurement && ReferenceEquals(measurement.Context.Values, context.Values)) measurement.End();
    }

    /// <summary>
    /// Marks only the real commit's append notifications as transactional.
    /// </summary>
    /// <returns>A flow-local lease.</returns>
    internal static IDisposable? Committing()
    {
        if (Current is null) return null;
        var previous = _committing.Value;
        _committing.Value = previous + 1;
        return new CommitLease(previous);
    }

    /// <summary>
    /// Captures the only added request-path RPC with a bounded wait.
    /// </summary>
    /// <returns>The capture task; failures never propagate.</returns>
    internal async Task CaptureBoundary()
    {
        if (EventLog is null) return;
        try
        {
            Boundary = await EventLog.GetTailSequenceNumber().WaitAsync(TailTimeout);
        }
        catch (Exception failure)
        {
            Unavailable(failure.GetType().Name);
        }
    }

    /// <summary>
    /// Classifies a dependency without replacing its registration or reading a model.
    /// </summary>
    /// <param name="parameter">The resolved parameter.</param>
    /// <param name="isReadModel">Whether Chronicle backs the parameter.</param>
    internal void Parameter(ParameterInfo parameter, bool isReadModel) => Try(() =>
    {
        var type = parameter.ParameterType;
        if (isReadModel)
        {
            var advisory = parameter.GetCustomAttribute<UnprotectedAttribute>() ?? type.GetCustomAttribute<UnprotectedAttribute>();
            lock (_gate)
            {
                _advisory[type] = advisory is null ? null : advisory.Reason ?? "Declared advisory";
                _explicitLookups[type] = parameter.IsDefined(typeof(LookUpByAttribute), true);
            }
            Classify(type, advisory?.Reason, advisory is not null);
            return;
        }
        var api = typeof(IReadModels).IsAssignableFrom(type) || typeof(IEventStore).IsAssignableFrom(type) || typeof(IEventSequence).IsAssignableFrom(type)
            ? type.Name
            : type.IsGenericType && type.GetGenericTypeDefinition().FullName == "MongoDB.Driver.IMongoCollection`1" ? "IMongoCollection" : null;
        if (api is not null)
        {
            Record("refused_read", type, api);
        }
        else if (!typeof(IDecisionReads).IsAssignableFrom(type))
        {
            Record("unknown_dependency", type, "Outside the read-protection guarantee");
        }
    });

    /// <summary>
    /// Records the key actually used by the stored-read path, with local Chronicle admission.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <param name="key">The actual stored key.</param>
    internal void StoredRead(Type model, ReadModelKey key) => Try(() =>
    {
        var description = Describe(model);
        string? advisory;
        bool explicitLookup;
        lock (_gate)
        {
            _advisory.TryGetValue(model, out advisory);
            _explicitLookups.TryGetValue(model, out explicitLookup);
        }
        if (!explicitLookup) CommandReadModelLookup.ObserveRedirect(Context, model, key);
        if (advisory is null && description.Admission.IsAdmitted && !description.UsesPinnedGenerationDelivery)
        {
            AddRead(new(model, key.Value, description.EventTypes, null, null, null));
        }
    });

    /// <summary>
    /// Observes a decision-read call without repeating it or enrolling an additional token.
    /// </summary>
    /// <typeparam name="T">The requested model.</typeparam>
    /// <param name="reader">The actual Chronicle reader.</param>
    /// <param name="source">The requested source.</param>
    /// <param name="streamType">The requested stream type.</param>
    /// <param name="streamId">The requested stream.</param>
    /// <param name="sourceType">The requested source type.</param>
    /// <param name="detached">Whether the call is detached.</param>
    internal void DecisionRead<T>(IDecisionReads reader, EventSourceId source, EventStreamType? streamType = null, EventStreamId? streamId = null, EventSourceType? sourceType = null, bool detached = false)
        where T : class => Try(() =>
        {
            var description = reader.Describe<T>();
            if (streamType is not null) description = description with { Admission = reader.AdmitForStream<T>() };
            if (detached)
            {
                Record("refused_read", typeof(T), "DetachedReadWhileReadsAreProtected");
            }
            else if (description.Admission.IsAdmitted && !description.UsesPinnedGenerationDelivery)
            {
                Record("protected_read", typeof(T), "Would be protected through IDecisionReads");
                AddRead(new(typeof(T), source, description.EventTypes, sourceType, streamType, streamId));
            }
            else
            {
                Record("refused_read", typeof(T), Refusal(description));
            }
        });

    /// <summary>
    /// Observes an already computed scope, never resolving a second strategy result.
    /// </summary>
    /// <param name="label">The scope's label.</param>
    /// <param name="scope">The actual scope.</param>
    /// <param name="reason">Its origin.</param>
    /// <param name="isRead">Whether a stream decision acquired the scope.</param>
    internal void Scope(EventSourceId label, ConcurrencyScope scope, string reason, bool isRead = false) => Try(() =>
    {
        lock (_gate)
        {
            if (_scopes.Count >= 128)
            {
                Unavailable("Scope observation limit");
                return;
            }
            _scopes.Add((label, scope, reason));
            foreach (var read in _reads.Where(_ => _.Model is not null && _.Source == label)) ObserveDisplacement(read, scope, reason);
        }
        if (isRead) AddRead(new(null, scope.EventSourceId, scope.EventTypes?.ToArray() ?? [], scope.EventSourceType, scope.EventStreamType, scope.EventStreamId));
    });

    /// <summary>
    /// Releases notifications and schedules an immutable scan after the invocation exits.
    /// </summary>
    internal void End()
    {
        Try(() => _subscription?.Dispose());
        Try(() =>
        {
            if (CommandTransaction.TryGetActive(out _))
            {
                // A borrowed transaction has not committed yet. Do not mistake its owner's future appends for
                // foreign events or infer a position range; classification remains available, sampling does not.
                Unavailable("Nested invocation has no committed position snapshot");
                return;
            }
            if (!_unavailable && Boundary is { } boundary && EventLog is { } log && Random.Shared.NextDouble() < SampleRate)
            {
                MeasuredRead[] reads;
                EventSequenceNumber[] own;
                lock (_gate)
                {
                    reads = _reads.ToArray();
                    own = _ownPositions.ToArray();
                }
                if (reads.Length > 0 && Queue?.Enqueue(new(Context.Type, log, boundary, reads, own, ScanLimit, ScanTimeout, Logger)) != true) Unavailable("Scan queue unavailable");
            }
        });
        _ended = true;
        _current.Value = _previous;
    }

    /// <summary>
    /// Contains every instrumentation failure and names the missing observation.
    /// </summary>
    /// <param name="action">The instrumentation, never application code.</param>
    internal void Try(Action action)
    {
        try { action(); }
        catch (Exception failure) { Unavailable(failure.GetType().Name); }
    }

    static (string? Type, string? Id) StreamDimensions(MeasuredRead read) =>
        (read.StreamType is { IsAll: false } type ? type.Value : null, read.StreamId is { IsDefault: false } id ? id.Value : null);

    static string Refusal(DecisionReadDescription description) => description.UsesPinnedGenerationDelivery ? "GenerationDeliveryNotSupported" : description.Admission.Reason?.ToString() ?? "Admission unavailable";

    void Initialize(IServiceProvider? services)
    {
        if (services is null)
        {
            Unavailable("No service provider");
            return;
        }
        Logger = services.GetService<ILogger<ReadProtectionMeasurement>>();
        EventLog = services.GetService<IEventLog>();
        Queue = services.GetService<ReadProtectionMeasureQueue>();
        var configuration = services.GetService<IConfiguration>();
        if (double.TryParse(configuration?["Arc:ReadProtection:MeasureTailTimeout"], out var milliseconds) && double.IsFinite(milliseconds)) TailTimeout = TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 1, 5000));
        if (double.TryParse(configuration?["Arc:ReadProtection:MeasureScanTimeout"], out var scanMilliseconds) && double.IsFinite(scanMilliseconds)) ScanTimeout = TimeSpan.FromMilliseconds(Math.Clamp(scanMilliseconds, 1, 5000));
        if (int.TryParse(configuration?["Arc:ReadProtection:MeasureScanLimit"], out var limit)) ScanLimit = Math.Clamp(limit, 1, 500);
        if (double.TryParse(configuration?["Arc:ReadProtection:MeasureSampleRate"], out var rate) && double.IsFinite(rate)) SampleRate = Math.Clamp(rate, 0, 1);
        if (EventLog is null)
        {
            Unavailable("No event log");
            return;
        }
        _subscription = EventLog.AppendOperations.Subscribe(appended => Try(() => ObserveAppends(appended)));
        DecisionReads = services.GetService<IEventStore>()?.GetDecisionReads();
    }

    void ObserveAppends(IEnumerable<AppendedEventWithResult> appended)
    {
        // Notifications run on the producer's async flow. Correlation is deliberately NOT attribution evidence.
        var active = Current;
        while (active is not null && !ReferenceEquals(active, this)) active = active._previous;
        if (active is null) return;
        lock (_gate)
        {
            foreach (var item in appended)
            {
                if (_committing.Value == 0) Record("immediate_append", item.Event.Content.GetType(), "Would be refused while reads are protected");
                if (!item.Result.IsSuccess)
                {
                    Record("conflict_today", reason: "Existing append failure");
                    continue;
                }
                if (item.Event.Context.SequenceNumber.IsActualValue)
                {
                    if (_ownPositions.Count >= 4096)
                    {
                        Unavailable("Own-position observation limit");
                        return;
                    }
                    _ownPositions.Add(item.Event.Context.SequenceNumber);
                }
                foreach (var read in _reads.Where(_ => _.Matches(item.Event.Context))) Record("self_write", read.Model, $"Own event at {item.Event.Context.SequenceNumber} matches the read; excluded from foreign conflicts");
            }
        }
    }

    void Classify(Type model, string? reason, bool advisory)
    {
        if (advisory)
        {
            Record("advisory_read", model, reason ?? "Declared advisory");
            return;
        }
        var description = Describe(model);
        Record(description.Admission.IsAdmitted && !description.UsesPinnedGenerationDelivery ? "protected_read" : "refused_read", model, description.Admission.IsAdmitted && !description.UsesPinnedGenerationDelivery ? "Would be protected" : Refusal(description));
    }

    DecisionReadDescription Describe(Type model) => (DecisionReadDescription)typeof(IDecisionReads).GetMethod(nameof(IDecisionReads.Describe))!.MakeGenericMethod(model).Invoke(DecisionReads, null)!;

    void AddRead(MeasuredRead read)
    {
        lock (_gate)
        {
            if (_reads.Count >= 128)
            {
                Unavailable("Read observation limit");
                return;
            }
            if (read.Model is not null && read.Source is { IsSpecified: true } source &&
                _reads.Exists(previous => previous.Model is not null && previous.Source?.Value == source.Value && StreamDimensions(previous) != StreamDimensions(read)))
            {
                Record("coarsened", read.Model, "stream_dims_dropped");
            }
            _reads.Add(read);
            foreach (var scope in _scopes.Where(_ => read.Model is not null && _.Label == read.Source)) ObserveDisplacement(read, scope.Scope, scope.Reason);
        }
    }

    void ObserveDisplacement(MeasuredRead read, ConcurrencyScope scope, string reason)
    {
        Record("displacement", read.Model, $"{reason} alongside a same-source read would need reconciliation under On");
        var narrower = (scope.EventStreamType is { IsAll: false } type && type != read.StreamType) ||
            (scope.EventStreamId is { IsDefault: false } id && id != read.StreamId) ||
            (scope.EventSourceType is { } sourceType && sourceType != EventSourceType.Default && sourceType != EventSourceType.Unspecified && sourceType != read.SourceType);
        var later = Boundary is { } boundary && scope.SequenceNumber.IsActualValue && (!boundary.IsActualValue || scope.SequenceNumber > boundary);
        if (narrower || later || scope == ConcurrencyScope.None || scope == ConcurrencyScope.NotSet || scope.IsIncomplete)
        {
            Record("scope_refusal", read.Model, $"{reason} is narrower than the read, later than G0, or incomplete; would be refused under On");
        }
    }

    void Unavailable(string reason)
    {
        _unavailable = true;
        Record("measurement_unavailable", reason: reason);
    }

    void Record(string category, Type? model = null, string? reason = null) => ReadProtectionTelemetry.Record(Context.Type, category, model, reason, Logger);

    sealed class CommitLease(int previous) : IDisposable
    {
        public void Dispose() => _committing.Value = previous;
    }
}
