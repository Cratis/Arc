// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            });
        }

        return services;
    }
}
