// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<CommandPipeline>();
        services.AddSingleton<ICommandPipeline>(sp => sp.GetRequiredService<CommandPipeline>());
        services.AddSingleton<ICommandPipelineWithCancellation>(sp => sp.GetRequiredService<CommandPipeline>());
        services.AddSingleton<ICommandFilters, CommandFilters>();
        services.AddSingleton<ICommandHandlerProviders, CommandHandlerProviders>();
        services.AddSingleton<ICommandResponseValueHandlers, CommandResponseValueHandlers>();
        services.AddSingleton<ICommandProvideInvoker, CommandProvideInvoker>();
        services.AddSingleton<ICommandHandlerArgumentResolver, CommandHandlerArgumentResolver>();
        services.AddTransient(sp => sp.GetRequiredService<ICommandContextAccessor>().Current);

        return services;
    }
}
