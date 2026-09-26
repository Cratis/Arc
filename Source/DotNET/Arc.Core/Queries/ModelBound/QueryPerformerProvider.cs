// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Represents a provider for query performers that are model bound.
/// </summary>
public class QueryPerformerProvider : IQueryPerformerProvider
{
    readonly Dictionary<FullyQualifiedQueryName, IQueryPerformer> _performers;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryPerformerProvider"/> class.
    /// </summary>
    /// <param name="types">The types to scan for read models.</param>
    /// <param name="queryMetadataRegistry">The registry of compile-time generated query metadata.</param>
    /// <param name="serviceProviderIsService">Service to determine if a type is registered as a service.</param>
    /// <param name="authorizationEvaluator">The authorization evaluator.</param>
    public QueryPerformerProvider(ITypes types, IQueryMetadataRegistry queryMetadataRegistry, IServiceProviderIsService serviceProviderIsService, IAuthorizationEvaluator authorizationEvaluator)
        : this(
            types,
            queryMetadataRegistry,
            (type, name, method) => new ModelBoundQueryPerformer(type, name, method, serviceProviderIsService, authorizationEvaluator))
    {
    }

    /// <summary>
    /// Constructs discovered performers with authorization resolved from the executing query scope.
    /// </summary>
    /// <param name="types">Discovered types.</param>
    /// <param name="queryMetadataRegistry">Generated query metadata.</param>
    /// <param name="serviceProviderIsService">Service classification.</param>
    /// <param name="scopeFactory">A scope for standalone performer calls.</param>
    /// <param name="resolveEvaluator">Resolves the evaluator in the executing scope.</param>
    internal QueryPerformerProvider(
        ITypes types,
        IQueryMetadataRegistry queryMetadataRegistry,
        IServiceProviderIsService serviceProviderIsService,
        IServiceScopeFactory scopeFactory,
        Func<IServiceProvider, IAuthorizationEvaluator> resolveEvaluator)
        : this(
            types,
            queryMetadataRegistry,
            (type, name, method) => new ModelBoundQueryPerformer(type, name, method, serviceProviderIsService, scopeFactory, resolveEvaluator))
    {
    }

    QueryPerformerProvider(
        ITypes types,
        IQueryMetadataRegistry queryMetadataRegistry,
        Func<Type, string, MethodInfo, ModelBoundQueryPerformer> createPerformer)
    {
        var generatedMetadata = queryMetadataRegistry.All;
        if (generatedMetadata.Count > 0)
        {
            _performers = CreatePerformersFromGeneratedMetadata(generatedMetadata, createPerformer)
                .ToDictionary(p => p.FullyQualifiedName, p => (IQueryPerformer)p);
            return;
        }

        var readModelTypes = types.All.Where(t => t.IsReadModel());
        _performers = readModelTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => ModelBoundQueryMethod.IsCandidate(m) && m.IsValidQueryFor(t))
                .Select(m => createPerformer(t, t.FullName ?? t.Name, m)))
            .ToDictionary(p => p.FullyQualifiedName, p => (IQueryPerformer)p);
    }

    /// <inheritdoc/>
    public IEnumerable<IQueryPerformer> Performers => _performers.Values;

    /// <inheritdoc/>
    public bool TryGetPerformerFor(FullyQualifiedQueryName query, [NotNullWhen(true)] out IQueryPerformer? performer) =>
        _performers.TryGetValue(query, out performer);

    static IEnumerable<ModelBoundQueryPerformer> CreatePerformersFromGeneratedMetadata(
        IDictionary<string, Type> generatedMetadata,
        Func<Type, string, MethodInfo, ModelBoundQueryPerformer> createPerformer)
    {
        foreach (var (fullyQualifiedQueryName, readModelType) in generatedMetadata)
        {
            var lastDotIndex = fullyQualifiedQueryName.LastIndexOf('.');
            if (lastDotIndex < 0 || lastDotIndex >= fullyQualifiedQueryName.Length - 1)
            {
                continue;
            }

            var readModelTypeName = fullyQualifiedQueryName[..lastDotIndex];
            var queryMethodName = fullyQualifiedQueryName[(lastDotIndex + 1)..];

            var method = readModelType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == queryMethodName && ModelBoundQueryMethod.IsCandidate(m) && m.IsValidQueryFor(readModelType));

            if (method is null)
            {
                continue;
            }

            yield return createPerformer(readModelType, readModelTypeName, method);
        }
    }
}
