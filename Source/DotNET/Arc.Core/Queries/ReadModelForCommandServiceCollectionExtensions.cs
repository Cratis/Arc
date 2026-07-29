// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for registering command-scoped read model resolution.
/// </summary>
public static class ReadModelForCommandServiceCollectionExtensions
{
    /// <summary>
    /// Registers a provider's read model types for command-scoped, by-key resolution.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="resolver">The <see cref="ICanResolveReadModelForCommand"/> that owns and resolves the read model types.</param>
    /// <returns>The service collection for continuation.</returns>
    /// <remarks>
    /// For each read model type the resolver owns, a scoped factory is registered that delegates to the resolver, so the
    /// command pipeline resolves the read model from DI by type like any other dependency. The set of registered read
    /// model types is additive: multiple providers can contribute their own types and the classification of a missing
    /// read model as invalid client input sees the union across all of them.
    /// </remarks>
    public static IServiceCollection AddReadModelsForCommand(this IServiceCollection services, ICanResolveReadModelForCommand resolver)
    {
        foreach (var readModelType in resolver.ReadModelTypes)
        {
            services.RemoveAll(readModelType);
            services.AddScoped(readModelType, serviceProvider =>
            {
                // Resolve the read model from the same scope the dependency is being resolved in, so the resolver can
                // reach scoped collaborators (the Chronicle IReadModels, the EF Core DbContext) through the context.
                var commandContext = serviceProvider.GetRequiredService<CommandContext>() with { ServiceProvider = serviceProvider };
                return resolver.Resolve(readModelType, commandContext).GetAwaiter().GetResult()!;
            });
        }

        var existing = services
            .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(RegisteredReadModelTypes))?
            .ImplementationInstance as RegisteredReadModelTypes;

        var union = (existing?.Types ?? [])
            .Concat(resolver.ReadModelTypes)
            .Distinct()
            .ToArray();

        services.RemoveAll<RegisteredReadModelTypes>();
        services.AddSingleton(new RegisteredReadModelTypes(union));

        return services;
    }
}
