// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <inheritdoc/>
    public async Task<QueryResult> Perform(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var receipt = OperationContextScope.BeginPipeline(serviceProvider);
        return await PerformCore(queryName, arguments, paging, sorting, serviceProvider, null, cancellationToken);
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
        try
        {
            return await PerformHostedCore(queryName, arguments, paging, sorting, requestServices, cancellationToken);
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

    static string DisplayNameOf(FullyQualifiedQueryName queryName)
    {
        var separator = queryName.Value.LastIndexOf('.');
        return separator < 0 ? queryName.Value : queryName.Value[(separator + 1)..];
    }

    static string TransportOf(QueryResult result)
    {
        if (result.Data is null)
        {
            return result.IsSuccess ? WellKnownTelemetryNames.SnapshotTransport : WellKnownTelemetryNames.UnknownTransport;
        }

        var dataType = result.Data.GetType();
        return dataType.ImplementsOpenGeneric(typeof(ISubject<>)) || dataType.ImplementsOpenGeneric(typeof(IAsyncEnumerable<>))
            ? WellKnownTelemetryNames.ObservableTransport
            : WellKnownTelemetryNames.SnapshotTransport;
    }

    static void RecordQuery(Activity? activity, IServiceProvider serviceProvider, string? queryName, string transport, string outcome, QueryResult? result, TimeSpan elapsed)
    {
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(WellKnownTelemetryNames.QueryTransport, transport);
            if (result?.AuthorizedTenant is { } tenant && tenant != TenantId.NotSet)
            {
                activity.SetTag(WellKnownTelemetryNames.Tenant, tenant.Value);
            }
            else
            {
                OperationActivity.AddResolvedTenant(activity, serviceProvider);
            }
        }

        OperationActivity.RecordOutcome(activity, WellKnownTelemetryNames.QueryOutcome, outcome, result?.ValidationResults ?? []);
        serviceProvider.GetService<PipelineMetrics>()?.RecordQuery(queryName ?? WellKnownTelemetryNames.Other, transport, outcome, elapsed);
    }

    async Task<QueryResult> PerformHostedCore(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider requestServices, CancellationToken cancellationToken)
    {
        if (!queryPerformerProviders.TryGetPerformersFor(queryName, out var performer))
        {
            return await PerformCore(queryName, arguments, paging, sorting, requestServices, null, cancellationToken);
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
            return await PerformCore(queryName, arguments, paging, sorting, requestServices, null, cancellationToken);
        }

        var evaluation = requestServices.GetService<AuthorizationEvaluation>() ??
            throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable.");
        var prepared = await evaluation.Prepare(target, requestServices, cancellationToken);
        if (!prepared.PrincipalChanged)
        {
            return await PerformCore(queryName, arguments, paging, sorting, requestServices, prepared, cancellationToken);
        }

        var scope = requestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        try
        {
            using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
            var result = await PerformCore(queryName, arguments, paging, sorting, scope.ServiceProvider, prepared, cancellationToken);
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

    async Task<QueryResult> PerformCore(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider serviceProvider, PreparedAuthorization? prepared, CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId();
        var started = Stopwatch.GetTimestamp();
        using var span = activitySource.Perform(queryName.Value);

        // Only a query that exists is named: the name arrives from the caller, and an unknown one must neither label
        // the span nor add a metric series.
        var known = queryPerformerProviders.TryGetPerformersFor(queryName, out _);
        var knownQueryName = known ? queryName.Value : null;
        OperationActivity.Describe(span.Activity, known ? DisplayNameOf(queryName) : span.Activity?.DisplayName ?? string.Empty, correlationId);
        if (knownQueryName is not null)
        {
            span.Activity?.SetTag(WellKnownTelemetryNames.QueryName, knownQueryName);
        }

        try
        {
            var result = await PerformObserved(queryName, arguments, paging, sorting, serviceProvider, prepared, correlationId, span.Activity, cancellationToken);
            RecordQuery(span.Activity, serviceProvider, knownQueryName, TransportOf(result), OperationOutcomes.For(result), result, Stopwatch.GetElapsedTime(started));
            return result;
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(span.Activity, ex);
            RecordQuery(span.Activity, serviceProvider, knownQueryName, WellKnownTelemetryNames.UnknownTransport, WellKnownOperationOutcomes.Error, null, Stopwatch.GetElapsedTime(started));
            throw;
        }
    }

    async Task<QueryResult> PerformObserved(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider serviceProvider, PreparedAuthorization? prepared, CorrelationId correlationId, Activity? activity, CancellationToken cancellationToken)
    {
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
            result.MergeWith(QueryResult.WithValidationError(correlationId, ex.ParameterName, ex.Message));
        }
        catch (Exception ex) when (ex is Cratis.Arc.Validation.IValidationFailure)
        {
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
}
