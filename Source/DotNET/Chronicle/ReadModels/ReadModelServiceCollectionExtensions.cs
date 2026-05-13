// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

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
    static readonly MethodInfo _releaseWithSubjectMethod = typeof(IReadModels)
        .GetMethods()
        .Single(_ =>
        {
            var parameters = _.GetParameters();
            return _.Name == nameof(IReadModels.Release) &&
                   _.IsGenericMethodDefinition &&
                   parameters.Length == 2 &&
                   parameters[0].ParameterType == typeof(Subject) &&
                   parameters[1].ParameterType.IsGenericParameter;
        });
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
            services.AddScoped(readModelType, serviceProvider => ResolveReadModel(
                readModelType,
                serviceProvider.GetRequiredService<CommandContext>(),
                serviceProvider.GetRequiredService<IReadModels>()));
        }

        return services;
    }

    /// <summary>
    /// Resolves a read model for the current command context.
    /// </summary>
    /// <param name="readModelType">Type of read model to resolve.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> to resolve from.</param>
    /// <param name="readModels">The <see cref="IReadModels"/> service.</param>
    /// <returns>The resolved read model instance.</returns>
    /// <exception cref="UnableToResolveReadModelFromCommandContext">Thrown when the command context does not contain a usable event source id.</exception>
    internal static object ResolveReadModel(Type readModelType, CommandContext commandContext, IReadModels readModels)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        if (eventSourceId == EventSourceId.Unspecified)
        {
            throw new UnableToResolveReadModelFromCommandContext(readModelType);
        }

        var readModel = readModels.GetInstanceById(readModelType, eventSourceId).GetAwaiter().GetResult();
        var subject = commandContext.GetSubject();

        return subject is null
            ? readModel
            : ReleaseReadModel(readModels, readModelType, subject, readModel);
    }

    static object ReleaseReadModel(IReadModels readModels, Type readModelType, Subject subject, object readModel)
    {
        try
        {
            var task = (Task)_releaseWithSubjectMethod
                .MakeGenericMethod(readModelType)
                .Invoke(readModels, [subject, readModel])!;

            task.GetAwaiter().GetResult();

            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to release read model '{readModelType.FullName}' with subject '{subject.Value}'.", exception);
        }
    }
}
