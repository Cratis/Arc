// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.Arc.MongoDB.Resilience;

/// <summary>
/// Represents an interceptor for <see cref="IMongoClient"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDatabaseInterceptor"/> class.
/// </remarks>
/// <param name="proxyGenerator"><see cref="ProxyGenerator"/> for creating further proxies.</param>
/// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
/// <param name="mongoClient"><see cref="IMongoClient"/> the interceptor is for.</param>
public class MongoDatabaseInterceptor(ProxyGenerator proxyGenerator, ResiliencePipeline resiliencePipeline, IMongoClient mongoClient) : IInterceptor
{
    static readonly ConcurrentDictionary<Type, Func<object, int, object>> _decorators = new();

    /// <summary>
    /// Gets the <see cref="ProxyGenerator"/> the interceptor was created with.
    /// </summary>
    /// <remarks>Collections are decorated without generating proxy types; the generator is kept for compatibility.</remarks>
    public ProxyGenerator ProxyGenerator => proxyGenerator;

    /// <summary>
    /// Gets the <see cref="ResiliencePipeline"/> the interceptor was created with.
    /// </summary>
    public ResiliencePipeline ResiliencePipeline => resiliencePipeline;

    /// <summary>
    /// Gets the <see cref="IMongoClient"/> the interceptor is for.
    /// </summary>
    public IMongoClient MongoClient => mongoClient;

    /// <inheritdoc/>
    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();

        // A generated proxy per document type cost a runtime type, generated under a lock, the first time each
        // document type was used, and a new proxy per call after that. A plain decorator costs neither.
        var collectionType = invocation.Method.ReturnType;
        var decorate = _decorators.GetOrAdd(collectionType, CreateDecorator);
        invocation.ReturnValue = decorate(invocation.ReturnValue!, mongoClient.Settings.MaxConnectionPoolSize / 2);
    }

    static Func<object, int, object> CreateDecorator(Type collectionType)
    {
        var documentType = collectionType.GetGenericArguments()[0];
        var decoratorType = typeof(ResilientMongoCollection<>).MakeGenericType(documentType);
        var inner = Expression.Parameter(typeof(object), "inner");
        var maxConcurrentCalls = Expression.Parameter(typeof(int), "maxConcurrentCalls");
        var constructor = decoratorType.GetConstructor([collectionType, typeof(int)])!;
        var create = Expression.New(constructor, Expression.Convert(inner, collectionType), maxConcurrentCalls);
        return Expression.Lambda<Func<object, int, object>>(Expression.Convert(create, typeof(object)), inner, maxConcurrentCalls).Compile();
    }
}
