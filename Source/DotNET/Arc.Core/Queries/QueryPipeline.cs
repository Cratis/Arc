// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Reflection;
using Cratis.Traces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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
        var correlationId = GetCorrelationId();
        var result = QueryResult.Success(correlationId);
        using var span = activitySource.Perform(queryName.Value);
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

            var dependencies = queryPerformer.Dependencies.Select(dependencyType => ResolveDependency(serviceProvider, dependencyType)).ToArray();
            var coercedArguments = CoerceArguments(arguments, queryPerformer);
            var context = new QueryContext(queryName, correlationId, paging, sorting, coercedArguments, dependencies, serviceProvider, cancellationToken);
            queryContextManager.Set(context);

            result = await queryFilters.OnPerform(context);
            if (!result.IsSuccess)
            {
                return result;
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
        catch (Exception ex)
        {
            result.MergeWith(QueryResult.Error(correlationId, ex));
        }

        return result;
    }

    /// <summary>
    /// Coerces each raw query argument to its declared parameter type before it reaches the filters and performer.
    /// </summary>
    /// <param name="arguments">The <see cref="QueryArguments"/> to coerce.</param>
    /// <param name="performer">The <see cref="IQueryPerformer"/> whose parameters describe the target types.</param>
    /// <returns>The coerced <see cref="QueryArguments"/>, or the original instance when nothing needed coercion.</returns>
    /// <remarks>
    /// One-shot transports coerce arguments at the HTTP boundary, but streaming transports (WebSocket / SSE observable
    /// queries) carry raw string arguments through verbatim. Coercing here — the single convergence point for every
    /// transport — guarantees the <see cref="QueryContext"/> always exposes arguments in their declared parameter types,
    /// so validation and invocation never see an unconverted string for a concept-typed parameter.
    /// The conversion is idempotent, so already-typed arguments pass through untouched.
    /// </remarks>
    static QueryArguments CoerceArguments(QueryArguments arguments, IQueryPerformer performer)
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
                var convertedValue = value.ConvertTo(parameter.Type);
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

    static object ResolveDependency(IServiceProvider serviceProvider, Type dependencyType)
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
