// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the operations on an observable query result that depend on the result's element type.
/// </summary>
/// <remarks>
/// An observable query returns its <see cref="ISubject{T}"/>, <see cref="IObservable{T}"/> or
/// <see cref="IAsyncEnumerable{T}"/> as an <see cref="object"/>, so its element type is only known at runtime. Rather
/// than building a generic method call through reflection at every site that handles such a result, the operations
/// are closed over the element type once, here, and cached per element type. Everything past this point is statically
/// typed, which keeps trimming and NativeAOT analysis to this one place. Element types registered by the Arc source
/// generator are used first; under the JIT any other element type is closed through reflection, which is not
/// supported in ahead-of-time compiled code.
/// </remarks>
internal abstract class ObservableQueryElementOperations
{
    static readonly ConcurrentDictionary<Type, ObservableQueryElementOperations> _operationsByElementType = new();

    /// <summary>
    /// Gets the operations for an element type.
    /// </summary>
    /// <param name="elementType">The element type of the observable query result.</param>
    /// <returns>The <see cref="ObservableQueryElementOperations"/> for the element type.</returns>
    /// <exception cref="MissingObservableQueryElementType">Thrown when the element type is not registered and dynamic code is not supported.</exception>
    public static ObservableQueryElementOperations For(Type elementType)
    {
        if (_operationsByElementType.TryGetValue(elementType, out var operations))
        {
            return operations;
        }

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new MissingObservableQueryElementType(elementType);
        }

        return _operationsByElementType.GetOrAdd(elementType, CreateThroughReflection);
    }

    /// <summary>
    /// Finds the type argument of a single-parameter open generic interface that a type is or implements.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="openGenericInterface">The open generic interface, for instance <see cref="IObservable{T}"/>.</param>
    /// <returns>The type argument of the first matching interface, or <see langword="null"/> if there is none.</returns>
    public static Type? FindElementType(Type type, Type openGenericInterface) =>
        (type.IsGenericType && type.GetGenericTypeDefinition() == openGenericInterface
            ? type
            : type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == openGenericInterface))?.GetGenericArguments()[0];

    /// <summary>
    /// Creates the WebSocket client observable for a subject.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="subject">The subject.</param>
    /// <returns>The <see cref="IClientObservable"/>.</returns>
    public abstract IClientObservable CreateClientObservable(IServiceProvider serviceProvider, QueryContext queryContext, object subject);

    /// <summary>
    /// Creates the Server-Sent Events client observable for a subject.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="subject">The subject.</param>
    /// <returns>The <see cref="IClientObservable"/>.</returns>
    public abstract IClientObservable CreateClientObservableSSE(IServiceProvider serviceProvider, QueryContext queryContext, object subject);

    /// <summary>
    /// Creates the WebSocket client observable for an async enumerable.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="enumerable">The async enumerable.</param>
    /// <returns>The <see cref="IClientEnumerableObservable"/>.</returns>
    public abstract IClientEnumerableObservable CreateClientEnumerableObservable(IServiceProvider serviceProvider, QueryContext queryContext, object enumerable);

    /// <summary>
    /// Creates the Server-Sent Events client observable for an async enumerable.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="enumerable">The async enumerable.</param>
    /// <returns>The <see cref="IClientEnumerableObservable"/>.</returns>
    public abstract IClientEnumerableObservable CreateClientEnumerableObservableSSE(IServiceProvider serviceProvider, QueryContext queryContext, object enumerable);

    /// <summary>
    /// Creates the HTTP response for an observable.
    /// </summary>
    /// <param name="context">The query and request state for the HTTP response.</param>
    /// <param name="observable">The observable.</param>
    /// <returns>The <see cref="ObservableQueryHttpResponse"/> to send.</returns>
    public abstract Task<ObservableQueryHttpResponse> CreateHttpResponse(ObservableQueryHttpContext context, object observable);

    /// <summary>
    /// Subscribes a demultiplexed connection to a subject.
    /// </summary>
    /// <param name="demultiplexer">The <see cref="ObservableQueryDemultiplexer"/> owning the subscription.</param>
    /// <param name="subscription">The <see cref="ObservableQuerySubscriptionContext"/> describing the subscription.</param>
    /// <param name="subject">The subject.</param>
    /// <returns>The subscription.</returns>
    public abstract IDisposable SubscribeToSubject(
        ObservableQueryDemultiplexer demultiplexer,
        ObservableQuerySubscriptionContext subscription,
        object subject);

    /// <summary>
    /// Streams an async enumerable to a demultiplexed connection.
    /// </summary>
    /// <param name="demultiplexer">The <see cref="ObservableQueryDemultiplexer"/> owning the stream.</param>
    /// <param name="subscription">The <see cref="ObservableQuerySubscriptionContext"/> describing the stream.</param>
    /// <param name="enumerable">The async enumerable.</param>
    /// <param name="guardServiceProvider">The <see cref="IServiceProvider"/> for emission guards, if any are registered.</param>
    /// <returns>Awaitable task.</returns>
    public abstract Task StreamAsyncEnumerable(
        ObservableQueryDemultiplexer demultiplexer,
        ObservableQuerySubscriptionContext subscription,
        object enumerable,
        IServiceProvider? guardServiceProvider);

    /// <summary>
    /// Registers the operations for an element type, keeping the first registration.
    /// </summary>
    /// <param name="elementType">The element type of the observable query result.</param>
    /// <param name="operations">The <see cref="ObservableQueryElementOperations"/> for the element type.</param>
    internal static void Register(Type elementType, ObservableQueryElementOperations operations) =>
        _operationsByElementType.TryAdd(elementType, operations);

    [RequiresDynamicCode("Closes the observable query operations over an element type only known at runtime.")]
    static ObservableQueryElementOperations CreateThroughReflection(Type type) =>
        (ObservableQueryElementOperations)typeof(ObservableQueryElementOperations)
            .GetMethod(nameof(CreateOperations), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type)
            .Invoke(null, null)!;

    static ObservableQueryElementOperationsFor<TElement> CreateOperations<TElement>() => new();
}
