// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Execution;
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
/// <param name="dependencyResolvers">The collection of <see cref="IQueryDependencyResolver"/> used to resolve specialized dependencies.</param>
public class QueryPipeline(
    ICorrelationIdAccessor correlationIdAccessor,
    IQueryContextManager queryContextManager,
    IQueryFilters queryFilters,
    IQueryPerformerProviders queryPerformerProviders,
    IQueryRenderers queryRenderers,
    IEnumerable<IQueryDependencyResolver> dependencyResolvers) : IQueryPipeline
{
    /// <inheritdoc/>
    public async Task<QueryResult> Perform(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider serviceProvider)
    {
        var correlationId = GetCorrelationId();
        var result = QueryResult.Success(correlationId);
        try
        {
            if (!queryPerformerProviders.TryGetPerformersFor(queryName, out var queryPerformer))
            {
                return QueryResult.MissingPerformer(correlationId, queryName);
            }

            var dependencies = queryPerformer.Dependencies.Select(type => ResolveDependency(type, arguments, serviceProvider));
            var context = new QueryContext(queryName, correlationId, paging, sorting, arguments, dependencies);
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
            result.Data = rendererResult.Data;
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

    object ResolveDependency(Type type, QueryArguments arguments, IServiceProvider serviceProvider)
    {
        foreach (var resolver in dependencyResolvers)
        {
            if (resolver.CanResolve(type))
            {
                var resolved = resolver.Resolve(type, arguments, serviceProvider);
                resolved.RethrowError();
                return resolved.AsT0;
            }
        }

        return serviceProvider.GetRequiredService(type);
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
}
