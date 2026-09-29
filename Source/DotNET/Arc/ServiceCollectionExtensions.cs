// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Commands;
using Cratis.Arc.ModelBinding;
using Cratis.Arc.Queries;
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
    const string MvcIsNotTrimCompatible = "MVC controllers are not supported with trimming or NativeAOT. Use model-bound commands and queries, and turn controllers off with WithoutControllers() on the Arc builder.";

    /// <summary>
    /// Add all controllers from all project referenced assemblies.
    /// </summary>
    /// <remarks>
    /// This is the standalone registration for hosts that do not use <c>AddCratisArc</c>: it registers the Arc
    /// services for ASP.NET Core together with MVC and controller discovery. Do not combine it with
    /// <c>AddCratisArc</c>, which registers the same services itself, and do not use it to turn controllers back on
    /// after <c>WithoutControllers</c>; that would register the Arc middlewares and JSON configuration a second time.
    /// </remarks>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovery.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    [RequiresUnreferencedCode(MvcIsNotTrimCompatible)]
    [RequiresDynamicCode(MvcIsNotTrimCompatible)]
    public static IServiceCollection AddControllersFromProjectReferencedAssembles(this IServiceCollection services, ITypes types)
    {
        var discoverableValidators = services.AddArcAspNetCore(types);
        services.AddArcControllers(discoverableValidators);
        return services;
    }

    /// <summary>
    /// Adds the Arc services for ASP.NET Core that do not depend on MVC: the request context and correlation id
    /// middlewares, validator discovery, the JSON options for minimal APIs and the ASP.NET Core authorization
    /// services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovery.</param>
    /// <returns>The <see cref="IDiscoverableValidators"/> registered, for MVC to validate with the same instance.</returns>
    internal static IDiscoverableValidators AddArcAspNetCore(this IServiceCollection services, ITypes types)
    {
        var discoverableValidators = new DiscoverableValidators(types);
        services.AddSingleton<IDiscoverableValidators>(discoverableValidators);
        services.AddTransient<IStartupFilter, ArcStartupFilter>();
        services.AddTransient<HttpRequestContextMiddleware>();
        services.AddCorrelationId();

        // Arc's authorization policy runtime and introspection guard depend on IAuthorizationPolicyProvider and
        // IAuthorizationService, which AddControllers used to register. AddAuthorization uses TryAdd, so it is a
        // no-op alongside MVC.
        services.AddAuthorization();

        services.AddSingleton<IPostConfigureOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>, ConfigureHttpJsonOptionsFromArcOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions);

        return discoverableValidators;
    }

    /// <summary>
    /// Adds MVC with Arc's filters, model binding and validation, and discovers the controllers in the project
    /// referenced assemblies.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> MVC validates with.</param>
    internal static void AddArcControllers(this IServiceCollection services, IDiscoverableValidators discoverableValidators)
    {
        services
            .AddActivitySource<CommandActionFilter>(Internals.ActivitySourceName)
            .AddActivitySource<QueryActionFilter>(Internals.ActivitySourceName);

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
                options.Conventions.Add(new ExcludeFromDiscoveryConvention());
            });

        services.AddSingleton<IPostConfigureOptions<JsonOptions>, ConfigureJsonOptionsFromArcOptions>();

        foreach (var controllerAssembly in ProjectReferencedAssemblies.Instance.Assemblies.Where(_ => _.DefinedTypes.Any(type => type.Implements(typeof(ControllerBase)))))
        {
            controllerBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(controllerAssembly));
        }
    }
}
