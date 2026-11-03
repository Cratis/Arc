// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for adding Cratis command handling services to a service collection.
/// </summary>
public static class CommandServiceCollectionExtensions
{
    /// <summary>
    /// Adds Cratis command handling services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCratisCommands(this IServiceCollection services)
    {
        services.AddSingleton<CommandContextManager>();
        services.AddSingleton<ICommandContextModifier>(sp => sp.GetRequiredService<CommandContextManager>());
        services.AddSingleton<ICommandContextAccessor>(sp => sp.GetRequiredService<CommandContextManager>());
        services.AddHostedService<ReadProtectionStartup>();
        services.AddSingleton<CommandPipeline>();
        services.AddSingleton<ICommandPipeline>(sp => sp.GetRequiredService<CommandPipeline>());
        services.AddSingleton<ICommandPipelineWithCancellation>(sp => sp.GetRequiredService<CommandPipeline>());
        services.AddSingleton<ICommandFilters, CommandFilters>();
        services.AddSingleton<ICommandHandlerProviders, CommandHandlerProviders>();
        services.TryAddSingleton(sp => new ArtifactAssemblyFilter(sp.GetService<IOptions<ArcOptions>>() ?? Microsoft.Extensions.Options.Options.Create(new ArcOptions())));
        services.AddSingleton<CommandHandlerProvider>(sp => new CommandHandlerProvider(
            sp.GetRequiredService<ITypes>(),
            sp.GetRequiredService<ArtifactAssemblyFilter>()));
        var modelBoundProvider = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(ICommandHandlerProvider) &&
            descriptor.ImplementationType == typeof(CommandHandlerProvider));
        if (modelBoundProvider is not null)
        {
            var index = services.IndexOf(modelBoundProvider);
            services[index] = ServiceDescriptor.Singleton<ICommandHandlerProvider>(sp => sp.GetRequiredService<CommandHandlerProvider>());
        }
        services.AddSingleton<ICommandResponseValueHandlers, CommandResponseValueHandlers>();
        services.AddSingleton<ICommandProvideInvoker, CommandProvideInvoker>();
        services.AddSingleton<ICommandHandlerArgumentResolver, CommandHandlerArgumentResolver>();
        services.AddTransient(sp => sp.GetRequiredService<ICommandContextAccessor>().Current);

        return services;
    }
}
