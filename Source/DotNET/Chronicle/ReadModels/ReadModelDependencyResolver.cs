// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="ICommandDependencyResolver"/> that resolves read model instances
/// from Chronicle's <see cref="IReadModels"/> service instead of requiring per-type DI registration.
/// </summary>
/// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> for discovering read model types.</param>
public class ReadModelDependencyResolver(IClientArtifactsProvider clientArtifactsProvider) : ICommandDependencyResolver
{
    readonly HashSet<Type> _readModelTypes = BuildReadModelTypes(clientArtifactsProvider);

    /// <inheritdoc/>
    public bool CanResolve(Type type) => _readModelTypes.Contains(type);

    /// <inheritdoc/>
    public Catch<object> Resolve(Type type, object command, CommandContextValues values, IServiceProvider serviceProvider)
    {
        var readModels = serviceProvider.GetRequiredService<IReadModels>();

        if (!values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var value) || value is not EventSourceId eventSourceId)
        {
            return Catch<object>.Failed(new UnableToResolveReadModelFromCommandContext(type));
        }

        if (eventSourceId == EventSourceId.Unspecified)
        {
            return Catch<object>.Failed(new UnableToResolveReadModelFromCommandContext(type));
        }

        return Catch<object>.Success(readModels.GetInstanceById(type, eventSourceId).GetAwaiter().GetResult());
    }

    static HashSet<Type> BuildReadModelTypes(IClientArtifactsProvider provider)
    {
        var fromProjections = provider.Projections
            .Select(projectionType =>
            {
                var projectionInterface = projectionType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionFor<>));

                return projectionInterface?.GetGenericArguments()[0];
            })
            .Where(type => type?.IsClass == true && !type.IsAbstract)
            .Cast<Type>();

        var modelBound = provider.ModelBoundProjections
            .Where(type => type.IsClass && !type.IsAbstract);

        return [.. fromProjections.Concat(modelBound).Distinct()];
    }
}
