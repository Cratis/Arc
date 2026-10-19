// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Queries;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Observability;

/// <summary>
/// Records the telemetry of one observable query subscription: that it is active, and each result pushed to it.
/// </summary>
/// <remarks>
/// <para>
/// The subscription counts as active from <see cref="Start"/> until <see cref="Dispose"/>, and ends exactly once
/// however many times it is disposed. A subscription that was not counted when it started, because nothing listened,
/// is not subtracted when it ends, so a listener attaching in between never sees a negative count.
/// </para>
/// <para>
/// The query name is recorded only when it matches a query Arc knows. A name taken from a client is
/// <see cref="WellKnownTelemetryNames.Other"/> otherwise, and never recorded as sent.
/// </para>
/// </remarks>
internal sealed class ObservableQueryTelemetry : IDisposable
{
    readonly PipelineMetrics? _metrics;
    readonly ActivitySource? _source;
    readonly IQueryPerformerProviders? _queryPerformerProviders;
    readonly string _queryName;
    readonly string _transport;
    readonly ActivityContext _subscriptionContext;
    string? _recordedName;
    int _started;
    int _counted;
    int _ended;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableQueryTelemetry"/> class.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> to find the metrics, activity source and known queries in, if any.</param>
    /// <param name="queryName">The fully qualified name of the query, as requested.</param>
    /// <param name="transport">The transport the results are delivered over.</param>
    internal ObservableQueryTelemetry(IServiceProvider? services, string queryName, string transport)
    {
        _metrics = services?.GetService<PipelineMetrics>();
        _source = services?.GetService<IActivitySource<QueryPipeline>>()?.ActualSource;
        _queryPerformerProviders = services?.GetService<IQueryPerformerProviders>();
        _queryName = queryName;
        _transport = transport;
        _subscriptionContext = Activity.Current?.Context ?? default;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Volatile.Read(ref _counted) == 1 && Interlocked.Exchange(ref _ended, 1) == 0)
        {
            _metrics!.RecordSubscription(ResolveName(), _transport, -1);
        }
    }

    /// <summary>
    /// Counts the subscription as active.
    /// </summary>
    internal void Start()
    {
        if (_metrics?.SubscriptionsEnabled != true || Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        Volatile.Write(ref _counted, 1);
        _metrics.RecordSubscription(ResolveName(), _transport, 1);
    }

    /// <summary>
    /// Starts the span for a result about to be pushed to the subscriber.
    /// </summary>
    /// <returns>The <see cref="Push"/> to complete once the result is written.</returns>
    /// <remarks>
    /// Call this once the emission is admitted, so that an emission a guard drops records nothing.
    /// </remarks>
    internal Push BeginPush()
    {
        if (_metrics is null && _source is null)
        {
            return default;
        }

        Activity? activity = null;
        if (_source?.HasListeners() == true)
        {
            var name = ResolveName();
            var tags = new ActivityTagsCollection
            {
                { WellKnownTelemetryNames.QueryName, name },
                { WellKnownTelemetryNames.QueryTransport, _transport }
            };
            activity = _source.StartActivity(
                WellKnownTelemetryNames.QueryPushSpan,
                ActivityKind.Internal,
                _subscriptionContext,
                tags,
                CausingContext());
        }

        return new Push(this, activity);
    }

    string ResolveName()
    {
        if (_recordedName is null)
        {
            var known = _queryPerformerProviders?.TryGetPerformersFor(new FullyQualifiedQueryName(_queryName), out _) == true;
            _recordedName = known && _metrics is not null ? _metrics.LimitQueryName(_queryName) : WellKnownTelemetryNames.Other;
        }

        return _recordedName;
    }

    /// <summary>
    /// Gets the link to the activity that caused the change, when the push runs inside one.
    /// </summary>
    /// <returns>The links, or <see langword="null"/> when the push runs in the subscription's own context.</returns>
    /// <remarks>
    /// The subscription's own context, the request it began in, is the parent already.
    /// </remarks>
    IEnumerable<ActivityLink>? CausingContext()
    {
        var current = Activity.Current;
        if (current is null || current.Context == _subscriptionContext)
        {
            return null;
        }

        return [new ActivityLink(current.Context)];
    }

    void Pushed()
    {
        if (_metrics?.PushesEnabled == true)
        {
            _metrics.RecordPush(ResolveName(), _transport);
        }
    }

    /// <summary>
    /// Represents the span and count of one result pushed to a subscriber.
    /// </summary>
    internal readonly struct Push : IDisposable
    {
        readonly ObservableQueryTelemetry? _telemetry;
        readonly Activity? _activity;

        /// <summary>
        /// Initializes a new instance of the <see cref="Push"/> struct.
        /// </summary>
        /// <param name="telemetry">The <see cref="ObservableQueryTelemetry"/> the push is for.</param>
        /// <param name="activity">The span of the push, if anything listens.</param>
        internal Push(ObservableQueryTelemetry telemetry, Activity? activity)
        {
            _telemetry = telemetry;
            _activity = activity;
        }

        /// <inheritdoc/>
        public void Dispose() => _activity?.Dispose();

        /// <summary>
        /// Counts the push, once the result has reached the subscriber.
        /// </summary>
        internal void Delivered() => _telemetry?.Pushed();
    }
}
