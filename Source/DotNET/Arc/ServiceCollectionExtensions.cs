// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.ModelBinding;
using Cratis.Arc.Validation;
using Cratis.Reflection;
using Cratis.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="ServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all controllers from all project referenced assemblies.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovery.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddControllersFromProjectReferencedAssembles(this IServiceCollection services, ITypes types)
    {
        var discoverableValidators = new DiscoverableValidators(types);
        services.AddSingleton<IDiscoverableValidators>(discoverableValidators);
        services.AddTransient<IStartupFilter, ArcStartupFilter>();
        services.AddTenancy();
        services.AddCorrelationId();

        // Register the command validation route convention
        services.AddSingleton<IApplicationModelProvider, CommandValidationRouteConvention>();

        var controllerBuilder = services
            .AddControllers(options =>
            {
                var bodyModelBinderProvider = options.ModelBinderProviders.First(_ => _ is BodyModelBinderProvider) as BodyModelBinderProvider;
                var complexObjectModelBinderProvider = options.ModelBinderProviders.First(_ => _ is ComplexObjectModelBinderProvider) as ComplexObjectModelBinderProvider;
                options.ModelBinderProviders.Insert(0, new FromRequestModelBinderProvider(bodyModelBinderProvider!, complexObjectModelBinderProvider!));
                options.AddValidation(discoverableValidators);
                options.AddCQRS();
            });

        services.AddSingleton<IPostConfigureOptions<JsonOptions>, ConfigureJsonOptionsFromArcOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions);

        foreach (var controllerAssembly in ProjectReferencedAssemblies.Instance.Assemblies.Where(_ => _.DefinedTypes.Any(type => type.Implements(typeof(ControllerBase)))))
        {
            controllerBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(controllerAssembly));
        }

        return services;
    }
}
