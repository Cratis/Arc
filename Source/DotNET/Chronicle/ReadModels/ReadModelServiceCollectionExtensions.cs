// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for read models.
/// </summary>
public static class ReadModelServiceCollectionExtensions
{
    static readonly MethodInfo _releaseMethod = typeof(IReadModels)
        .GetMethods()
        .Single(_ =>
        {
            var parameters = _.GetParameters();
            return _.Name == nameof(IReadModels.Release) &&
                   _.IsGenericMethodDefinition &&
                   parameters.Length == 1 &&
                   parameters[0].ParameterType.IsGenericParameter;
        });

    /// <summary>
    /// Adds read model auto-discovery and registration to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> for client artifacts.</param>
    /// <returns>The service collection for continuation.</returns>
    /// <remarks>
    /// A read model is injectable into command-scoped code (a <c>CommandValidator&lt;&gt;</c>, <c>Provide()</c>, or
    /// <c>Handle()</c>) because it is resolvable by key (the resolved event source id) through <see cref="IReadModels"/>.
    /// What makes a read model resolvable that way is a Chronicle backing artifact, so this registers every read model
    /// that has a projection, model-bound projection, or reducer — independent of whether it also carries the Arc-level
    /// <c>[ReadModel]</c> attribute. It deliberately does not register read models by <c>[ReadModel]</c> alone: that
    /// attribute is an Arc concept that does not imply Chronicle key resolution, and a read model backed by another
    /// provider (for example Entity Framework Core) is registered by that provider, not here.
    /// </remarks>
    public static IServiceCollection AddReadModels(this IServiceCollection services, IClientArtifactsProvider clientArtifactsProvider)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IInterceptReadModel<>), typeof(ReadModelInterceptor<>)));

        var modelBoundReadModels = clientArtifactsProvider.ModelBoundProjections
            .Where(type => type.IsClass && !type.IsAbstract);

        // A read model is registered for command-scope resolution because it is resolvable by key through
        // IReadModels. That resolvability comes from a Chronicle backing artifact, so the set is the union of the
        // read model types behind each backing kind. Adding a future backing kind is one more ReadModelTargetsFrom.
        var readModelTypes = ReadModelTargetsFrom(clientArtifactsProvider.Projections, typeof(IProjectionFor<>))
            .Concat(modelBoundReadModels)
            .Concat(ReadModelTargetsFrom(clientArtifactsProvider.Reducers, typeof(IReducerFor<>)))
            .Distinct()
            .ToArray();
        foreach (var readModelType in readModelTypes)
        {
            services.RemoveAll(readModelType);
            services.AddScoped(readModelType, serviceProvider => ResolveReadModel(
                readModelType,
                serviceProvider.GetRequiredService<CommandContext>(),
                serviceProvider.GetRequiredService<IReadModels>())!);
        }

        return services;
    }

    /// <summary>
    /// Resolves a read model for the current command context.
    /// </summary>
    /// <param name="readModelType">Type of read model to resolve.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> to resolve from.</param>
    /// <param name="readModels">The <see cref="IReadModels"/> service.</param>
    /// <returns>The resolved read model instance, or null when it does not exist.</returns>
    /// <exception cref="UnableToResolveReadModelFromCommandContext">Thrown when the command context does not contain a usable event source id.</exception>
    internal static object? ResolveReadModel(Type readModelType, CommandContext commandContext, IReadModels readModels)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        if (eventSourceId == EventSourceId.Unspecified)
        {
            throw new UnableToResolveReadModelFromCommandContext(readModelType);
        }

        var readModel = readModels.GetInstanceById(readModelType, eventSourceId).GetAwaiter().GetResult();
        var subject = commandContext.GetSubject();

        // A never-created or removed read model resolves to null; there is nothing to release (decrypt),
        // and releasing null would dereference it while resolving the compliance subject. Hand back the
        // null so command-side code can inject a nullable read model and treat null as "does not exist".
        return subject is null || readModel is null
            ? readModel
            : ReleaseReadModel(readModels, readModelType, readModel);
    }

    /// <summary>
    /// Extracts the read model target types behind a set of backing artifacts that implement a given open generic
    /// interface (for example <see cref="IProjectionFor{T}"/> or <see cref="IReducerFor{T}"/>).
    /// </summary>
    /// <param name="artifactTypes">The backing artifact types to inspect.</param>
    /// <param name="openGenericInterface">The open generic interface whose single type argument is the read model type.</param>
    /// <returns>The concrete read model types behind the artifacts.</returns>
    static IEnumerable<Type> ReadModelTargetsFrom(IEnumerable<Type> artifactTypes, Type openGenericInterface) =>
        artifactTypes
            .Select(artifactType => artifactType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
                ?.GetGenericArguments()[0])
            .Where(type => type?.IsClass == true && !type.IsAbstract)
            .Cast<Type>();

    static object ReleaseReadModel(IReadModels readModels, Type readModelType, object readModel)
    {
        try
        {
            var task = (Task)_releaseMethod
                .MakeGenericMethod(readModelType)
                .Invoke(readModels, [readModel])!;

            task.GetAwaiter().GetResult();

            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to release read model '{readModelType.FullName}'.", exception);
        }
    }
}
