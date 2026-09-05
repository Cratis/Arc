// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Maps identity provider endpoints using the provided endpoint mapper.
/// </summary>
public static class IdentityEndpointMapper
{
    const string GetIdentityDetailsSchemaEndpointName = "GetIdentityDetailsSchema";
    const string GetIdentityDetailsEndpointName = "GetIdentityDetails";
    const string GetUsersEndpointName = "GetUsers";
    const string GetTenantsEndpointName = "GetTenants";

    /// <summary>
    /// Maps the identity provider endpoint.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    public static void MapIdentityProviderEndpoint(this IEndpointMapper mapper, IServiceProvider serviceProvider)
    {
        var serviceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
        var hasIdentityDetailsProvider = serviceProviderIsService?.IsService(typeof(IProvideIdentityDetails)) == true;

        if (!mapper.EndpointExists(GetIdentityDetailsSchemaEndpointName))
        {
            var schemaMetadata = new EndpointMetadata(
                GetIdentityDetailsSchemaEndpointName,
                "Get current user identity details schema",
                ["Cratis Identity"],
                AllowAnonymous: true,
                ResponseType: typeof(JsonNode));

            mapper.MapGet(
                "/.cratis/identity-details/schema",
                async context =>
                {
                    var identityDetailsProvider = context.RequestServices.GetService<IProvideIdentityDetails>();
                    if (identityDetailsProvider is null)
                    {
                        await context.WriteResponseAsJson(new JsonObject(), typeof(JsonObject), context.RequestAborted);
                        return;
                    }

                    var detailsType = identityDetailsProvider.GetType()
                        .GetInterfaces()
                        .FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IProvideIdentityDetails<>))
                        ?.GetGenericArguments()
                        .SingleOrDefault() ?? typeof(object);

                    var jsonSerializerOptions = new JsonSerializerOptions(context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions);
                    jsonSerializerOptions.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
                    var schema = jsonSerializerOptions.GetJsonSchemaAsNode(detailsType);
                    await context.WriteResponseAsJson(schema, schema.GetType(), context.RequestAborted);
                },
                schemaMetadata);
        }

        if (!hasIdentityDetailsProvider)
        {
            MapUsersEndpoint(mapper);
            MapTenantsEndpoint(mapper);
            return;
        }

        if (mapper.EndpointExists(GetIdentityDetailsEndpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            GetIdentityDetailsEndpointName,
            "Get current user identity details",
            ["Cratis Identity"],
            AllowAnonymous: true);

        mapper.MapGet(
            "/.cratis/me",
            async context =>
            {
                var identityProvider = context.RequestServices.GetRequiredService<IIdentityProvider>();
                var result = await identityProvider.Get();

                if (!result.IsAuthenticated)
                {
                    context.StatusCode = 401;
                    return;
                }

                if (!result.IsAuthorized)
                {
                    context.StatusCode = 403;
                    return;
                }

                await identityProvider.SetCookieForHttpResponse(result);
            },
            metadata);

        MapUsersEndpoint(mapper);
        MapTenantsEndpoint(mapper);
    }

    static void MapUsersEndpoint(IEndpointMapper mapper)
    {
        if (mapper.EndpointExists(GetUsersEndpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            GetUsersEndpointName,
            "Get development users",
            ["Cratis Development"],
            AllowAnonymous: true,
            ResponseType: typeof(IEnumerable<User>));

        mapper.MapGet(
            "/.cratis/users",
            async context =>
            {
                var users = new List<User>();
                if (context.RequestServices.GetService<IInstancesOf<ICanProvideUsers>>() is IInstancesOf<ICanProvideUsers> providers)
                {
                    foreach (var provider in providers)
                    {
                        users.AddRange(await provider.Provide());
                    }
                }

                await context.WriteResponseAsJson(users, typeof(IEnumerable<User>), context.RequestAborted);
            },
            metadata);
    }

    static void MapTenantsEndpoint(IEndpointMapper mapper)
    {
        if (mapper.EndpointExists(GetTenantsEndpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            GetTenantsEndpointName,
            "Get development tenants",
            ["Cratis Development"],
            AllowAnonymous: true,
            ResponseType: typeof(IEnumerable<Tenant>));

        mapper.MapGet(
            "/.cratis/tenants",
            async context =>
            {
                var tenants = new List<Tenant>();
                if (context.RequestServices.GetService<IInstancesOf<ICanProvideTenants>>() is IInstancesOf<ICanProvideTenants> providers)
                {
                    foreach (var provider in providers)
                    {
                        tenants.AddRange(await provider.Provide());
                    }
                }

                await context.WriteResponseAsJson(tenants, typeof(IEnumerable<Tenant>), context.RequestAborted);
            },
            metadata);
    }
}
