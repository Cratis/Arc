// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Authorization;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Represents a provider for query performers that are model bound.
/// </summary>
public class QueryPerformerProvider : IQueryPerformerProvider
{
    static readonly ConditionalWeakTable<ITypes, QueryDiscovery> _queriesByUniverse = new();
    static readonly ConditionalWeakTable<IDictionary<string, Type>, GeneratedQueryDiscovery> _generatedQueries = new();

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
        var queries = generatedMetadata.Count > 0
            ? _generatedQueries.GetValue(generatedMetadata, _ => new GeneratedQueryDiscovery()).GetQueries(generatedMetadata)
            : _queriesByUniverse.GetValue(types, _ => new QueryDiscovery()).GetQueries(types);

        _performers = queries
            .Select(query => createPerformer(query.ReadModelType, query.ReadModelTypeName, query.Method))
            .ToDictionary(p => p.FullyQualifiedName, p => (IQueryPerformer)p);
    }

    /// <inheritdoc/>
    public IEnumerable<IQueryPerformer> Performers => _performers.Values;

    /// <inheritdoc/>
    public bool TryGetPerformerFor(FullyQualifiedQueryName query, [NotNullWhen(true)] out IQueryPerformer? performer) =>
        _performers.TryGetValue(query, out performer);

    readonly record struct DiscoveredQuery(Type ReadModelType, string ReadModelTypeName, MethodInfo Method);

    sealed class QueryDiscovery
    {
        readonly object _gate = new();
        Type[] _types = [];
        DiscoveredQuery[] _queries = [];
        bool _initialized;

        public DiscoveredQuery[] GetQueries(ITypes universe)
        {
            var types = universe.All.ToArray();
            lock (_gate)
            {
                if (!_initialized || !types.SequenceEqual(_types))
                {
                    _queries = types.Where(t => t.IsReadModel())
                        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                            .Where(m => m.IsValidQueryFor(t))
                            .Select(m => new DiscoveredQuery(t, t.FullName ?? t.Name, m)))
                        .ToArray();
                    _types = types;
                    _initialized = true;
                }

                return _queries;
            }
        }
    }

    sealed class GeneratedQueryDiscovery
    {
        readonly object _gate = new();
        KeyValuePair<string, Type>[] _snapshot = [];
        DiscoveredQuery[] _queries = [];

        public DiscoveredQuery[] GetQueries(IDictionary<string, Type> metadata)
        {
            var current = metadata.ToArray();
            lock (_gate)
            {
                if (!current.SequenceEqual(_snapshot))
                {
                    _queries = Discover(current).ToArray();
                    _snapshot = current;
                }

                return _queries;
            }
        }

        static IEnumerable<DiscoveredQuery> Discover(IEnumerable<KeyValuePair<string, Type>> metadata)
        {
            foreach (var (fullyQualifiedQueryName, readModelType) in metadata)
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
                    .FirstOrDefault(m => m.Name == queryMethodName && m.IsValidQueryFor(readModelType));

                if (method is not null)
                {
                    yield return new DiscoveredQuery(readModelType, readModelTypeName, method);
                }
            }
        }
    }
}
