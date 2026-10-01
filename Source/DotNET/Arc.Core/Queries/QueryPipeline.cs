// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Observability;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Reflection;
using Cratis.Traces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
public class QueryPipeline(
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
        var (knownName, nameResolved) = KnownNameWhenTraced(queryName);
        using var span = activitySource.Perform(knownName ?? WellKnownTelemetryNames.Other);
        var observation = BeginObservation(span.Activity, queryName, knownName, nameResolved);
        try
        {
            var result = await PerformCore(queryName, arguments, paging, sorting, serviceProvider, null, span.Activity, cancellationToken);
            Record(observation, serviceProvider, result, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(observation, serviceProvider, ex, cancellationToken);
            throw;
        }
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
    /// <returns>A result whose owned scope, if present, the caller must dispose after response or subscription completion.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">A target cannot be safely authorized.</exception>
    // Also called by Cratis.Arc.Testing through InternalsVisibleTo; keep its hosted authorization and scope-ownership contract compatible.
    internal async Task<QueryResult> PerformHosted(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider requestServices, CancellationToken cancellationToken)
    {
        using var receipt = OperationContextScope.BeginIfNotSet(requestServices);

        // The span starts before authorization is prepared, so a request turned away on the way in is traced and
        // measured like one turned away inside the pipeline.
        var (knownName, nameResolved) = KnownNameWhenTraced(queryName);
        using var span = activitySource.Perform(knownName ?? WellKnownTelemetryNames.Other);
        var observation = BeginObservation(span.Activity, queryName, knownName, nameResolved);
        try
        {
            var result = await PerformHostedGuarded(queryName, arguments, paging, sorting, requestServices, span.Activity, cancellationToken);
            Record(observation, requestServices, result, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(observation, requestServices, ex, cancellationToken);
            throw;
        }
    }

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

    static QueryObservation BeginObservation(Activity? activity, FullyQualifiedQueryName queryName, string? knownName, bool nameResolved)
    {
        if (knownName is not null && activity is { IsAllDataRequested: true })
        {
            activity.SetTag(WellKnownTelemetryNames.QueryName, knownName);
        }

        return new(activity, queryName, knownName, nameResolved, Stopwatch.GetTimestamp());
    }

    async Task<QueryResult> PerformHostedGuarded(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider requestServices, Activity? activity, CancellationToken cancellationToken)
    {
        try
        {
            return await PerformHostedCore(queryName, arguments, paging, sorting, requestServices, activity, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidAuthorizationConfiguration or AmbiguousAuthorizationLevel)
        {
            requestServices.GetService<ILogger<QueryPipeline>>()?.AuthorizationConfigurationFailed(exception);
            return QueryResult.Unauthorized(GetCorrelationId());
        }
        catch (Exception exception)
        {
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
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(WellKnownTelemetryNames.QueryTransport, transport);
            if (result is not null)
            {
                OperationActivity.AddCorrelationId(activity, result.CorrelationId);
            }

            if (result?.AuthorizedTenant is { } tenant && tenant != TenantId.NotSet)
            {
                activity.SetTag(WellKnownTelemetryNames.Tenant, tenant.Value);
            }
        }

        OperationActivity.RecordOutcome(activity, WellKnownTelemetryNames.QueryOutcome, outcome, result?.ValidationResults ?? []);
        if (measured)
        {
            var knownName = observation.NameResolved ? observation.KnownName : KnownNameOf(observation.QueryName);
            metrics!.RecordQuery(knownName ?? WellKnownTelemetryNames.Other, transport, outcome, Stopwatch.GetElapsedTime(observation.Started));
        }
    }

    async Task<QueryResult> PerformHostedCore(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider requestServices, Activity? activity, CancellationToken cancellationToken)
    {
        if (!queryPerformerProviders.TryGetPerformersFor(queryName, out var performer))
        {
            return await PerformCore(queryName, arguments, paging, sorting, requestServices, null, activity, cancellationToken);
        }

        var declarations = requestServices.GetService<AuthorizationDeclarations>() ??
            throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
        var target = QueryAuthorizationTarget.For(performer, declarations);
        var declaration = target switch
        {
            System.Reflection.MethodInfo method => declarations.For(method),
            Type type => declarations.For(type),
            _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
        };
        if (!declaration.RequiresAsynchronousEvaluation)
        {
            return await PerformCore(queryName, arguments, paging, sorting, requestServices, null, activity, cancellationToken);
        }

        var evaluation = requestServices.GetService<AuthorizationEvaluation>() ??
            throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable.");
        var prepared = await evaluation.Prepare(target, requestServices, cancellationToken);
        if (!prepared.PrincipalChanged)
        {
            return await PerformCore(queryName, arguments, paging, sorting, requestServices, prepared, activity, cancellationToken);
        }

        var scope = requestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        try
        {
            using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
            var result = await PerformCore(queryName, arguments, paging, sorting, scope.ServiceProvider, prepared, activity, cancellationToken);
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

    async Task<QueryResult> PerformCore(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider serviceProvider, PreparedAuthorization? prepared, Activity? activity, CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId();

        // A query performed from a protected command is not part of that command's decision; its own validators run.
        using var decisionQuery = CommandDecisionPolicy.BeginQuery();
        var result = QueryResult.Success(correlationId);
        using var principalLease = new AuthorizationPrincipalLease();
        try
        {
            if (paging.IsPaged)
            {
                var pagingValidation = await ValidatePaging(paging, correlationId);
                if (!pagingValidation.IsSuccess)
                {
                    return pagingValidation;
                }
            }

            if (!queryPerformerProviders.TryGetPerformersFor(queryName, out var queryPerformer))
            {
                return QueryResult.MissingPerformer(correlationId, queryName);
            }

            var coercedArguments = CoerceArguments(arguments, queryPerformer);
            var context = new QueryContext(queryName, correlationId, paging, sorting, coercedArguments, ServiceProvider: serviceProvider, CancellationToken: cancellationToken)
            {
                PreparedAuthorization = prepared,
                ReceivedAt = OperationContextScope.Current ?? default
            };

            // Install the prepared identity before any filter is discovered or constructed. Filter constructors can
            // resolve scoped, tenant-bound dependencies that must never be cached under the outer request identity.
            if (prepared?.PrincipalChanged == true && prepared.SelectedPrincipal is { } selectedPrincipal)
            {
                principalLease.Attach(serviceProvider.GetRequiredService<AuthorizationPrincipalScope>().Begin(selectedPrincipal, serviceProvider));
            }

            OperationActivity.AddResolvedTenant(activity, serviceProvider);

            if (queryFilters is IStagedQueryFilters stagedFilters)
            {
                queryContextManager.Set(context);
                result = await stagedFilters.Authorize(context);
                if (!result.IsSuccess)
                {
                    return result;
                }

                if (prepared is null && context.AuthorizedPrincipal is { } authorizedPrincipal)
                {
                    principalLease.Attach(serviceProvider.GetRequiredService<AuthorizationPrincipalScope>().Begin(authorizedPrincipal, serviceProvider));
                }

                cancellationToken.ThrowIfCancellationRequested();
                AuthorizationExecutionScopes.MarkWorkStarted(serviceProvider);
                var authorizedDependencies = queryPerformer.Dependencies.Select(dependencyType => ResolveDependency(serviceProvider, dependencyType)).ToArray();
                context = context with { Dependencies = authorizedDependencies };
                queryContextManager.Set(context);
                result.MergeWith(await stagedFilters.AfterAuthorization(context));
            }
            else
            {
                if (prepared?.Declaration.RequiresAsynchronousEvaluation == true)
                {
                    throw new InvalidAuthorizationConfiguration("Advanced query authorization requires Arc's staged authorization filters before dependency construction.");
                }

                // Preserve the contract of custom IQueryFilters implementations that expect dependencies on entry.
                AuthorizationExecutionScopes.MarkWorkStarted(serviceProvider);
                var dependencies = queryPerformer.Dependencies.Select(dependencyType => ResolveDependency(serviceProvider, dependencyType)).ToArray();
                context = context with { Dependencies = dependencies };
                queryContextManager.Set(context);
                result = await queryFilters.OnPerform(context);
                if (result.IsSuccess && prepared is null && context.AuthorizedPrincipal is { } authorizedPrincipal)
                {
                    principalLease.Attach(serviceProvider.GetRequiredService<AuthorizationPrincipalScope>().Begin(authorizedPrincipal, serviceProvider));
                }
            }

            if (!result.IsSuccess)
            {
                return result;
            }

            // Capture the filter's effective scope before the performer can mutate its original value.
            // Every emission reconstructs its own copy from this baseline.
            if (context.SubscriptionScope is { } subscriptionScope)
            {
                var serializerOptions = serviceProvider.GetService<IOptions<ArcOptions>>()?.Value.JsonSerializerOptions
                    ?? new ArcOptions().JsonSerializerOptions;
                context.SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(subscriptionScope, serializerOptions);
            }

            result.AuthorizedPrincipal = context.AuthorizedPrincipal;
            result.AuthorizedArguments = context.Arguments;
            result.AuthorizedQueryContext = context;
            if (context.AuthorizedPrincipal is not null)
            {
                result.AuthorizedTenant = serviceProvider.GetRequiredService<TenantIdAccessor>().Current;
            }
            cancellationToken.ThrowIfCancellationRequested();

            // Direct Perform calls do not prepare custom evaluator declarations without recognized attribute metadata.
            // Re-resolve even after a successful no-policy filter: a custom evaluator can change requirements on the
            // same target before invocation. Caching that absence could bypass a newly required policy. This costs
            // another declaration lookup on the no-policy path, but does not run a policy a second time.
            var declarations = serviceProvider.GetService<AuthorizationDeclarations>() ??
                throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
            var target = QueryAuthorizationTarget.For(queryPerformer, declarations);
            var declaration = target switch
            {
                MethodInfo method => declarations.For(method),
                Type type => declarations.For(type),
                _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
            };

            if ((declaration.RequiresAsynchronousEvaluation && context.AuthorizedExecution is null) ||
                (context.AuthorizedExecution is { } verdict &&
                 !verdict.IsCurrent(
                     target,
                     serviceProvider.GetRequiredService<ICurrentPrincipalAccessor>(),
                     declaration)))
            {
                return QueryResult.Unauthorized(correlationId);
            }

            var data = await queryPerformer.Perform(context);
            if (data is null)
            {
                return result;
            }
            var rendererResult = queryRenderers.Render(queryName, data, serviceProvider);
            if (rendererResult is null)
            {
                return QueryResult.Error(correlationId, "No renderer result");
            }
            result.Data = await ApplyInterceptors(queryPerformer.ReadModelType, rendererResult.Data, serviceProvider);
            result.Paging = context.Paging == Paging.NotPaged ? PagingInfo.NotPaged : new PagingInfo(
                        context.Paging.Page,
                        context.Paging.Size,
                        rendererResult.TotalItems);

            return result;
        }
        catch (MissingArgumentForQuery ex)
        {
            OperationActivity.RecordException(activity, ex);
            result.MergeWith(QueryResult.WithValidationError(correlationId, ex.ParameterName, ex.Message));
        }
        catch (Exception ex) when (ex is Cratis.Arc.Validation.IValidationFailure)
        {
            OperationActivity.RecordException(activity, ex);
            result.MergeWith(QueryResult.FromException(correlationId, ex));
        }
        catch (AuthorizationIdentityChanged)
        {
            return QueryResult.Unauthorized(correlationId);
        }
        catch (InvalidAuthorizationConfiguration ex)
        {
            result.MergeWith(QueryResult.Unauthorized(correlationId));
            result.ExceptionMessages = [.. result.ExceptionMessages, ex.Message];
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(activity, ex);
            result.MergeWith(QueryResult.Error(correlationId, ex));
        }

        return result;
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
            var items = queryable.Cast<object>().ToList();
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

        if (discoverableValidators.TryGet(typeof(PageNumber), out var pageNumberValidator))
        {
            var validationContext = new ValidationContext<PageNumber>(paging.Page);
            var validationResult = await pageNumberValidator.ValidateAsync(validationContext);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    result.MergeWith(QueryResult.WithValidationError(correlationId, nameof(Paging.Page), error.ErrorMessage));
                }
            }
        }

        if (discoverableValidators.TryGet(typeof(PageSize), out var pageSizeValidator))
        {
            var validationContext = new ValidationContext<PageSize>(paging.Size);
            var validationResult = await pageSizeValidator.ValidateAsync(validationContext);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    result.MergeWith(QueryResult.WithValidationError(correlationId, nameof(Paging.Size), error.ErrorMessage));
                }
            }
        }

        return result;
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
}
