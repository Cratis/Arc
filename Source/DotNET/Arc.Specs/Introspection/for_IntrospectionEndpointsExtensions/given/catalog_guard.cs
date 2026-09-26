// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions.given;

internal static class catalog_guard
{
    internal static Exception? Map(Action<WebApplicationBuilder> configure, string? roles = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        configure(builder);
        builder.AddCratisArc(options =>
        {
            options.Introspection.RequireAuthentication = true;
            options.Introspection.Roles = roles;
        });
        using var app = builder.Build();
        return Catch.Exception(() => app.MapIntrospectionEndpoints());
    }
}
