// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
#if NET9_0_OR_GREATER
using System.Reflection;
using System.Runtime.CompilerServices;
#endif
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Reflection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for aggregate roots.
/// </summary>
public static class AggregateRootServiceCollectionExtensions
{
    /// <summary>
    /// Adds aggregate root auto-discovery and registration to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types">The <see cref="ITypes"/> for type discovery.</param>
    /// <returns>The service collection for continuation.</returns>
    public static IServiceCollection AddAggregateRoots(this IServiceCollection services, ITypes types)
    {
        foreach (var aggregateRootType in types.All.Where(_ => _.HasInterface<IAggregateRoot>()).ToArray())
        {
            services.RemoveAll(aggregateRootType);
            services.AddScoped(aggregateRootType, serviceProvider =>
            {
                var commandContext = serviceProvider.GetRequiredService<CommandContext>();
                var aggregateRootFactory = serviceProvider.GetRequiredService<IAggregateRootFactory>();

                var eventSourceId = commandContext.GetEventSourceId();
                if (eventSourceId == EventSourceId.Unspecified)
                {
                    throw new UnableToResolveAggregateRootFromCommandContext(aggregateRootType);
                }

#if NET9_0_OR_GREATER
                if (!RuntimeFeature.IsDynamicCodeSupported)
                {
                    return GetAggregateRootWithoutDynamicCode(aggregateRootType, aggregateRootFactory, eventSourceId);
                }
#endif

                return GetAggregateRootThroughReflection(aggregateRootType, aggregateRootFactory, eventSourceId);
            });
        }

        return services;
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// Gets an aggregate root from the factory without closing a generic method through reflection over dynamic code.
    /// </summary>
    /// <param name="aggregateRootType">The type of aggregate root.</param>
    /// <param name="aggregateRootFactory">The <see cref="IAggregateRootFactory"/> to get it from.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the aggregate root.</param>
    /// <returns>The aggregate root.</returns>
    /// <exception cref="System.Reflection.TargetInvocationException">Thrown when the factory throws before it returns a task, with the failure as inner exception.</exception>
    internal static object GetAggregateRootWithoutDynamicCode(Type aggregateRootType, IAggregateRootFactory aggregateRootFactory, EventSourceId eventSourceId)
    {
        // The helper closes over a reference type only, which the runtime shares code for, so it needs no dynamic code. Invoking it
        // reproduces the exception type that the reflection path throws when the factory throws synchronously.
        var task = (Task<object>)typeof(AggregateRootServiceCollectionExtensions)
            .GetMethod(nameof(GetAggregateRootAsObject), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(aggregateRootType)
            .Invoke(null, [aggregateRootFactory, eventSourceId])!;

        return task.GetAwaiter().GetResult();
    }

    static Task<object> GetAggregateRootAsObject<TAggregateRoot>(IAggregateRootFactory factory, EventSourceId eventSourceId)
        where TAggregateRoot : class, IAggregateRoot =>
        AsObject(factory.Get<TAggregateRoot>(eventSourceId, null, null));

    static async Task<object> AsObject<T>(Task<T> task)
        where T : class => await task;
#endif

    [RequiresDynamicCode("Closes IAggregateRootFactory.Get over an aggregate root type only known at runtime.")]
    static object GetAggregateRootThroughReflection(Type aggregateRootType, IAggregateRootFactory aggregateRootFactory, EventSourceId eventSourceId)
    {
        var getMethod = typeof(IAggregateRootFactory)
            .GetMethods()
            .First(m => m.Name == nameof(IAggregateRootFactory.Get) && m.IsGenericMethod);

        var genericGetMethod = getMethod.MakeGenericMethod(aggregateRootType);

        // IAggregateRootFactory.Get is async (it rehydrates from the event stream), so Invoke returns a
        // Task<TAggregateRoot>. Unwrap it here so the resolved dependency is the aggregate root itself and
        // not the Task — the command handler argument resolver injects this value directly, without awaiting.
        var task = (Task)genericGetMethod.Invoke(aggregateRootFactory, [eventSourceId, null, null])!;
        task.GetAwaiter().GetResult();
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }
}
