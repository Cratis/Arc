// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
            // Closed on first resolution and reused, so discovery never fails startup and resolving reflects only once per type.
            var resolver = new Lazy<MethodInfo>(() => CloseResolverFor(aggregateRootType));
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

                return GetAggregateRoot(resolver.Value, aggregateRootFactory, eventSourceId);
            });
        }

        return services;
    }

    /// <summary>
    /// Closes the resolver helper over an aggregate root type, once per type at registration.
    /// </summary>
    /// <param name="aggregateRootType">The type of aggregate root.</param>
    /// <returns>The closed resolver to pass to <see cref="GetAggregateRoot"/>.</returns>
    /// <remarks>
    /// The helper closes over a reference type only, which the runtime shares code for, so it needs no dynamic code.
    /// </remarks>
    internal static MethodInfo CloseResolverFor(Type aggregateRootType) =>
        typeof(AggregateRootServiceCollectionExtensions)
            .GetMethod(nameof(GetAggregateRootAsObject), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(aggregateRootType);

    /// <summary>
    /// Gets an aggregate root from the factory through a resolver closed by <see cref="CloseResolverFor"/>.
    /// </summary>
    /// <param name="resolver">The closed resolver for the aggregate root type.</param>
    /// <param name="aggregateRootFactory">The <see cref="IAggregateRootFactory"/> to get it from.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the aggregate root.</param>
    /// <returns>The aggregate root itself, not the task the factory returns.</returns>
    /// <exception cref="TargetInvocationException">Thrown when the factory throws before it returns a task, with the failure as inner exception.</exception>
    internal static object GetAggregateRoot(MethodInfo resolver, IAggregateRootFactory aggregateRootFactory, EventSourceId eventSourceId)
    {
        // IAggregateRootFactory.Get is async (it rehydrates from the event stream). Unwrap it here so the resolved dependency
        // is the aggregate root itself and not the task: the command handler argument resolver injects this value directly.
        var task = (Task<object>)resolver.Invoke(null, [aggregateRootFactory, eventSourceId])!;
        return task.GetAwaiter().GetResult();
    }

    static Task<object> GetAggregateRootAsObject<TAggregateRoot>(IAggregateRootFactory factory, EventSourceId eventSourceId)
        where TAggregateRoot : class, IAggregateRoot =>
        AsObject(factory.Get<TAggregateRoot>(eventSourceId, null, null));

    static async Task<object> AsObject<T>(Task<T> task)
        where T : class => await task.ConfigureAwait(false);
}
