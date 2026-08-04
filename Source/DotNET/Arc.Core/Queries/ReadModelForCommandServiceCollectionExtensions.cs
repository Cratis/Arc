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
    /// For each read model type the resolver claims, a scoped factory is registered that delegates to the resolver, so the
    /// command pipeline resolves the read model from DI by type like any other dependency. The set of registered read
    /// model types is additive: multiple providers can contribute their own types and the classification of a missing
    /// read model as invalid client input sees the union across all of them.
    /// <para>
    /// Which types a provider claims follows from its <see cref="ICanResolveReadModelForCommand.Ownership"/> rather than
    /// from the order the application registers its providers in. A <see cref="ReadModelForCommandOwnership.Declared"/>
    /// provider claims every type it reports, taking over one another provider already resolves. A
    /// <see cref="ReadModelForCommandOwnership.Fallback"/> provider claims only the types nothing else resolves yet, and a
    /// declaring provider registered after it still takes those over — so a declaring provider wins either way round.
    /// </para>
    /// <para>
    /// The provider that wins also decides which serialization boundary the injected instance crosses, and the shipped
    /// providers cross entirely different ones: Chronicle deserializes a JSON payload with <c>System.Text.Json</c>,
    /// Entity Framework Core materializes through its own entity model, and MongoDB materializes through the driver's
    /// <c>BsonClassMap</c> and convention machinery. Whatever customization belongs to one of those boundaries — a
    /// convention pack, a class-map customization, an element rename, a custom serializer, a JSON converter — applies
    /// on the command side only when its own provider is the one that claimed the read model type.
    /// </para>
    /// <para>
    /// Chronicle and Entity Framework Core both declare, so in an application whose read models are owned by either,
    /// MongoDB never claims a command-side read model and no MongoDB serialization customization reaches one — however
    /// the MongoDB integration is configured, and in whatever order anything is registered. The same customization can
    /// still be plainly at work on the query side, which is what makes this worth stating rather than discovering.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddReadModelsForCommand(this IServiceCollection services, ICanResolveReadModelForCommand resolver)
    {
        var claimed = new List<Type>();
        foreach (var readModelType in resolver.ReadModelTypes)
        {
            if (resolver.Ownership == ReadModelForCommandOwnership.Declared)
            {
                services.RemoveAll(readModelType);
            }
            else if (services.Any(descriptor => descriptor.ServiceType == readModelType))
            {
                // A fallback provider fills gaps — it never takes over a read model type something else already resolves,
                // be that a declaring provider registered before it or the application's own registration.
                continue;
            }

            services.AddScoped(readModelType, serviceProvider =>
            {
                // Resolve the read model from the same scope the dependency is being resolved in, so the resolver can
                // reach scoped collaborators (the Chronicle IReadModels, the EF Core DbContext, the MongoDB collection)
                // through the context.
                var commandContext = serviceProvider.GetRequiredService<CommandContext>() with { ServiceProvider = serviceProvider };
                return resolver.Resolve(readModelType, commandContext).GetAwaiter().GetResult()!;
            });

            claimed.Add(readModelType);
        }

        var existing = services
            .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(RegisteredReadModelTypes))?
            .ImplementationInstance as RegisteredReadModelTypes;

        var union = (existing?.Types ?? [])
            .Concat(claimed)
            .Distinct()
            .ToArray();

        services.RemoveAll<RegisteredReadModelTypes>();
        services.AddSingleton(new RegisteredReadModelTypes(union));

        return services;
    }
}
