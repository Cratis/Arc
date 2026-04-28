// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Commands.ResponseValueHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddSingleton<ICommandPipeline, CommandPipeline>();
        services.AddSingleton<ICommandFilters, CommandFilters>();
        services.AddSingleton<ICommandHandlerProviders, CommandHandlerProviders>();
        services.AddSingleton<ICommandResponseValueHandlers, CommandResponseValueHandlers>();
        services.AddTransient(sp => sp.GetRequiredService<ICommandContextAccessor>().Current);

        services.TryAddSingleton<ICommandHandlerProvider, CommandHandlerProvider>();
        services.TryAddSingleton<ICommandContextValuesBuilder, CommandContextValuesBuilder>();
        services.AddSingleton<ICommandFilter, FluentValidationFilter>();
        services.AddSingleton<ICommandFilter, DataAnnotationValidationFilter>();
        services.AddSingleton<ICommandFilter, AuthorizationFilter>();
        services.AddSingleton<ICommandResponseValueHandler, ValidationResultResponseValueHandler>();
        services.AddSingleton<ICommandResponseValueHandler, AuthorizationResultResponseValueHandler>();

        return services;
    }
}