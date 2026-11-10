// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Observability;
using Cratis.Arc.Slices;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Reflection;
using Cratis.Traces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a query pipeline.
/// </summary>
/// <param name="correlationIdAccessor">Accessor for the current correlation ID.</param>
/// <param name="queryContextManager">Manages the current query context.</param>
/// <param name="queryFilters">The query filters.</param>
/// <param name="queryPerformerProviders">The query performer providers.</param>
/// <param name="queryRenderers">The query renderers.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> for intercepting read models.</param>
/// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> for validating paging and sorting.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
public partial class QueryPipeline(
    ICorrelationIdAccessor correlationIdAccessor,
    IQueryContextManager queryContextManager,
    IQueryFilters queryFilters,
    IQueryPerformerProviders queryPerformerProviders,
    IQueryRenderers queryRenderers,
    IReadModelInterceptors readModelInterceptors,
    IDiscoverableValidators discoverableValidators,
    IActivitySource<QueryPipeline> activitySource) : IQueryPipeline
{
    static readonly ConcurrentDictionary<Type, bool> _observableDataTypes = new();

    /// <inheritdoc/>
    public async Task<QueryResult> Perform(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var receipt = OperationContextScope.BeginPipeline(serviceProvider);
        var request = new QueryRequest(queryName, arguments, paging, sorting, serviceProvider);
        return await Observe(request, activity => PerformCore(request, null, activity, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Prepares HTTP or hub policy metadata once and creates a clean scope only if a scheme changes identity.
    /// </summary>
    /// <param name="queryName">The query name.</param>
    /// <param name="arguments">The query arguments.</param>
    /// <param name="paging">Paging.</param>
    /// <param name="sorting">Sorting.</param>
    /// <param name="requestServices">The existing request provider.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>A result whose scope the transport must release with <see cref="QueryResult.ReleaseScope"/> after response or subscription completion.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">A target cannot be safely authorized.</exception>
    // Also called by Cratis.Arc.Testing through InternalsVisibleTo; keep its hosted authorization and scope-ownership contract compatible.
    public async Task<QueryResult> PerformHosted(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider requestServices, CancellationToken cancellationToken)
    {
        using var receipt = OperationContextScope.BeginIfNotSet(requestServices);

        // The span starts before authorization is prepared, so a request turned away on the way in is traced and
        // measured like one turned away inside the pipeline.
        var request = new QueryRequest(queryName, arguments, paging, sorting, requestServices);
        return await Observe(request, activity => PerformHostedGuarded(request, activity, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Gets the slice location of the query a performer performs.
    /// </summary>
    /// <param name="performer">The <see cref="IQueryPerformer"/> performing the query.</param>
    /// <returns>The <see cref="SliceLocation"/>; <see cref="SliceLocation.NotSet"/> for a performer without a type, such as a substitute, so it never fails the query.</returns>
    static SliceLocation SliceLocationFor(IQueryPerformer performer) =>
        performer.Type is { } type ? SliceLocation.For(type) : SliceLocation.NotSet;

    static bool IsObservable(Type dataType) =>
        _observableDataTypes.GetOrAdd(dataType, static type => type.ImplementsOpenGeneric(typeof(ISubject<>)) || type.ImplementsOpenGeneric(typeof(IAsyncEnumerable<>)));

    static string TransportOf(QueryResult result)
    {
        if (result.Data is null)
        {
            return result.IsSuccess ? WellKnownTelemetryNames.SnapshotTransport : WellKnownTelemetryNames.UnknownTransport;
        }

        return IsObservable(result.Data.GetType()) ? WellKnownTelemetryNames.ObservableTransport : WellKnownTelemetryNames.SnapshotTransport;
    }

    static void RecordOnActivity(Activity? activity, QueryResult? result, string transport)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        activity.SetTag(WellKnownTelemetryNames.QueryTransport, transport);
        if (result is not null)
        {
            OperationActivity.AddCorrelationId(activity, result.CorrelationId);
        }

        if (result?.AuthorizedTenant is { } tenant && tenant != TenantId.NotSet)
        {
            activity.SetTag(WellKnownTelemetryNames.Tenant, tenant.Value)
                .SetTag(WellKnownTelemetryNames.TenantId, tenant.Value);
        }
    }

    static QueryObservation BeginObservation(Activity? activity, FullyQualifiedQueryName queryName, string? knownName, bool nameResolved)
    {
        if (knownName is not null && activity is { IsAllDataRequested: true })
        {
            activity.SetTag(WellKnownTelemetryNames.QueryName, knownName);
        }

        return new(activity, queryName, knownName, nameResolved, Stopwatch.GetTimestamp());
    }

    async Task<QueryResult> Observe(QueryRequest request, Func<Activity?, Task<QueryResult>> perform, CancellationToken cancellationToken)
    {
        var (knownName, nameResolved) = KnownNameWhenTraced(request.QueryName);
        using var span = activitySource.Perform(knownName ?? WellKnownTelemetryNames.Other);
        var observation = BeginObservation(span.Activity, request.QueryName, knownName, nameResolved);
        try
        {
            var result = await perform(span.Activity);
            Record(observation, request.ServiceProvider, result, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(observation, request.ServiceProvider, ex, cancellationToken);
            throw;
        }
    }

    async Task<QueryResult> PerformHostedGuarded(QueryRequest request, Activity? activity, CancellationToken cancellationToken)
    {
        var requestServices = request.ServiceProvider;
        try
        {
            return await PerformHostedCore(request, activity, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (UnauthorizedCause.IsCauseOf(exception))
        {
            OperationActivity.RecordException(activity, exception);
            return QueryResult.Unauthorized(GetCorrelationId());
        }
        catch (Exception exception) when (exception is InvalidAuthorizationConfiguration or AmbiguousAuthorizationLevel)
        {
            OperationActivity.RecordException(activity, exception);
            requestServices.GetService<ILogger<QueryPipeline>>()?.AuthorizationConfigurationFailed(exception);
            return QueryResult.Unauthorized(GetCorrelationId());
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(activity, exception);
            requestServices.GetService<ILogger<QueryPipeline>>()?.AuthorizationPreparationFailed(exception);
            return QueryResult.Error(GetCorrelationId(), "An error occurred while preparing authorization.");
        }
    }

    (string? KnownName, bool Resolved) KnownNameWhenTraced(FullyQualifiedQueryName queryName)
    {
        // Only a query that exists is named: the name arrives from the caller, and an unknown one must neither label
        // the span nor add a metric series. Nothing is looked up when nothing listens.
        if (!activitySource.ActualSource.HasListeners())
        {
            return (null, false);
        }

        return (KnownNameOf(queryName), true);
    }

    string? KnownNameOf(FullyQualifiedQueryName queryName) =>
        queryPerformerProviders.TryGetPerformersFor(queryName, out _) ? queryName.Value : null;

    void RecordFailure(QueryObservation observation, IServiceProvider serviceProvider, Exception exception, CancellationToken cancellationToken)
    {
        OperationActivity.RecordException(observation.Activity, exception);
        Record(observation, serviceProvider, null, cancellationToken);
    }

    void Record(QueryObservation observation, IServiceProvider serviceProvider, QueryResult? result, CancellationToken cancellationToken)
    {
        var activity = observation.Activity;
        var metrics = serviceProvider.GetService<PipelineMetrics>();
        var measured = metrics?.QueriesEnabled == true;
        if (activity is not { IsAllDataRequested: true } && !measured)
        {
            return;
        }

        var outcome = result is null ? OperationOutcomes.ForException(cancellationToken) : OperationOutcomes.For(result, cancellationToken);
        var transport = result is null ? WellKnownTelemetryNames.UnknownTransport : TransportOf(result);
        RecordOnActivity(activity, result, transport);
        OperationActivity.RecordOutcome(activity, WellKnownTelemetryNames.QueryOutcome, outcome, result?.ValidationResults ?? []);
        if (measured)
        {
            RecordMeasurement(metrics!, observation, transport, outcome);
        }
    }

    void RecordMeasurement(PipelineMetrics metrics, QueryObservation observation, string transport, string outcome)
    {
        var knownName = observation.NameResolved ? observation.KnownName : KnownNameOf(observation.QueryName);
        metrics.RecordQuery(knownName ?? WellKnownTelemetryNames.Other, transport, outcome, Stopwatch.GetElapsedTime(observation.Started));
    }

    async Task<QueryResult> PerformHostedCore(QueryRequest request, Activity? activity, CancellationToken cancellationToken)
    {
        var requestServices = request.ServiceProvider;
        if (!queryPerformerProviders.TryGetPerformersFor(request.QueryName, out var performer))
        {
            return await PerformCore(request, null, activity, cancellationToken);
        }

        var (target, declaration) = ResolveAuthorization(performer, requestServices);
        if (!declaration.RequiresAsynchronousEvaluation)
        {
            return await PerformCore(request, null, activity, cancellationToken);
        }

        var evaluation = requestServices.GetService<AuthorizationEvaluation>() ??
            throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable.");
        var prepared = await evaluation.Prepare(target, requestServices, cancellationToken);
        if (!prepared.PrincipalChanged)
        {
            return await PerformCore(request, prepared, activity, cancellationToken);
        }

        var scope = requestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        try
        {
            using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
            var result = await PerformCore(request with { ServiceProvider = scope.ServiceProvider }, prepared, activity, cancellationToken);
            result.OwnedScope = scope;
            return result;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Coerces each raw query argument to its declared parameter type before it reaches the filters and performer.
    /// </summary>
    /// <param name="arguments">The <see cref="QueryArguments"/> to coerce.</param>
    /// <param name="performer">The <see cref="IQueryPerformer"/> whose parameters describe the target types.</param>
    /// <returns>The coerced <see cref="QueryArguments"/>, or the original instance when nothing needed coercion.</returns>
    /// <exception cref="InvalidQueryArgument">A scalar or collection argument cannot be converted.</exception>
    /// <remarks>
    /// One-shot transports coerce arguments at the HTTP boundary, but streaming transports (WebSocket / SSE observable
    /// queries) carry raw string arguments through verbatim. Coercing here — the single convergence point for every
    /// transport — guarantees the <see cref="QueryContext"/> always exposes arguments in their declared parameter types,
    /// so validation and invocation never see an unconverted string for a concept-typed parameter.
    /// The conversion is idempotent, so already-typed arguments pass through untouched.
    /// </remarks>
    QueryArguments CoerceArguments(QueryArguments arguments, IQueryPerformer performer)
    {
        var parameters = performer.Parameters;
        if (arguments.Count == 0 || parameters is null)
        {
            return arguments;
        }

        var coerced = new QueryArguments();
        var changed = false;
        foreach (var kvp in arguments)
        {
            var value = kvp.Value;
            var parameter = parameters.FirstOrDefault(_ => string.Equals(_.Name, kvp.Key, StringComparison.OrdinalIgnoreCase));
            if (parameter is not null)
            {
                var convertedValue = value.ConvertQueryArgument(parameter.Type, parameter.Name, performer.FullyQualifiedName);

                if (convertedValue is not null && !ReferenceEquals(convertedValue, value))
                {
                    value = convertedValue;
                    changed = true;
                }
            }

            coerced[kvp.Key] = value;
        }

        return changed ? coerced : arguments;
    }

    object ResolveDependency(IServiceProvider serviceProvider, Type dependencyType)
    {
        try
        {
            return serviceProvider.GetRequiredService(dependencyType);
        }
        catch (InvalidOperationException failure)
        {
            // A query dependency (e.g. a Chronicle read model service) could not be resolved. Translate the raw
            // container exception into an actionable error rather than a bare "Unable to resolve service" message.
            throw new CannotResolveDependency(dependencyType, failure);
        }
    }

    CorrelationId GetCorrelationId()
    {
        var correlationId = correlationIdAccessor.Current;
        if (correlationId == CorrelationId.NotSet)
        {
            correlationId = CorrelationId.New();
        }

        return correlationId;
    }

    async Task<object> ApplyInterceptors(Type readModelType, object data, IServiceProvider serviceProvider)
    {
        // Streaming results are intercepted per emission by the streaming transport — ClientObservableSSE /
        // ClientObservable for single observable queries, and ObservableQueryDemultiplexer for the multiplexed
        // hub. The data here is the subject / async-enumerable wrapper, not a read model instance, so handing it
        // to the per-item interceptor would try to bind the wrapper to the read model type and throw.
        if (data.GetType().ImplementsOpenGeneric(typeof(ISubject<>)) ||
            data.GetType().ImplementsOpenGeneric(typeof(IAsyncEnumerable<>)))
        {
            return data;
        }

        if (data is IQueryable queryable)
        {
            var items = Enumerable.Cast<object>(queryable).ToList();
            return await readModelInterceptors.Intercept(readModelType, items, serviceProvider);
        }

        if (data is IEnumerable<object> enumerable)
        {
            return await readModelInterceptors.Intercept(readModelType, enumerable, serviceProvider);
        }

        var intercepted = await readModelInterceptors.Intercept(readModelType, [data], serviceProvider);
        return intercepted.First();
    }

    async Task<QueryResult> ValidatePaging(Paging paging, CorrelationId correlationId)
    {
        var result = QueryResult.Success(correlationId);
        await ValidatePagingValue(result, correlationId, paging.Page, nameof(Paging.Page));
        await ValidatePagingValue(result, correlationId, paging.Size, nameof(Paging.Size));
        return result;
    }

    async Task ValidatePagingValue<T>(QueryResult result, CorrelationId correlationId, T value, string property)
    {
        if (!discoverableValidators.TryGet(typeof(T), out var validator))
        {
            return;
        }

        var validationResult = await validator.ValidateAsync(new ValidationContext<T>(value));
        foreach (var error in validationResult.Errors)
        {
            result.MergeWith(QueryResult.WithValidationError(correlationId, property, error.ErrorMessage));
        }
    }

    /// <summary>
    /// Holds what is needed to record a query once it has run.
    /// </summary>
    /// <param name="Activity">The query span, if anything listens.</param>
    /// <param name="QueryName">The name of the query, as the caller gave it.</param>
    /// <param name="KnownName">The name of the query when it is known, or <see langword="null"/>.</param>
    /// <param name="NameResolved">Whether <paramref name="KnownName"/> was looked up.</param>
    /// <param name="Started">When the query started, as a <see cref="Stopwatch"/> timestamp.</param>
    readonly record struct QueryObservation(Activity? Activity, FullyQualifiedQueryName QueryName, string? KnownName, bool NameResolved, long Started);

    /// <summary>
    /// Holds what identifies one query call: the name, the arguments, the paging, the sorting and the provider to perform it with.
    /// </summary>
    /// <param name="QueryName">The name of the query.</param>
    /// <param name="Arguments">The raw arguments.</param>
    /// <param name="Paging">The paging.</param>
    /// <param name="Sorting">The sorting.</param>
    /// <param name="ServiceProvider">The provider the query is performed with.</param>
    readonly record struct QueryRequest(FullyQualifiedQueryName QueryName, QueryArguments Arguments, Paging Paging, Sorting Sorting, IServiceProvider ServiceProvider);
}
