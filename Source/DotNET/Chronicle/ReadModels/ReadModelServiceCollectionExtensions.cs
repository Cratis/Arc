// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for read models.
/// </summary>
public static class ReadModelServiceCollectionExtensions
{
    static bool _initialized;

    /// <summary>
    /// Adds read model auto-discovery and registration to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> for client artifacts.</param>
    /// <returns>The service collection for continuation.</returns>
    public static IServiceCollection AddReadModels(this IServiceCollection services, IClientArtifactsProvider clientArtifactsProvider)
    {
        if (_initialized)
        {
            return services;
        }
        _initialized = true;

        var readModelTypesFromProjections = clientArtifactsProvider.Projections
            .Select(projectionType =>
            {
                var projectionInterface = projectionType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionFor<>));
                return projectionInterface?.GetGenericArguments()[0];
            })
            .Where(type => type?.IsClass == true && !type.IsAbstract)
            .Cast<Type>();

        var modelBoundReadModels = clientArtifactsProvider.ModelBoundProjections
            .Where(type => type.IsClass && !type.IsAbstract);
        var readModelTypes = readModelTypesFromProjections
            .Concat(modelBoundReadModels)
            .Distinct()
            .ToArray();
        foreach (var readModelType in readModelTypes)
        {
            services.RemoveAll(readModelType);
            services.AddScoped(readModelType, serviceProvider =>
            {
                var commandContext = serviceProvider.GetRequiredService<CommandContext>();
                var readModels = serviceProvider.GetRequiredService<IReadModels>();

                var eventSourceId = commandContext.GetEventSourceId();
                if (eventSourceId == EventSourceId.Unspecified)
                {
                    throw new UnableToResolveReadModelFromCommandContext(readModelType);
                }

                return readModels.GetInstanceById(readModelType, eventSourceId).GetAwaiter().GetResult();
            });
        }

        return services;
    }
}
