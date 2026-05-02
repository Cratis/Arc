// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using TestApps.Authentication;
using TestApps.Features.QueryShowcase;

var builder = ArcApplication.CreateBuilder(args);

_ = typeof(ShowcaseItem);

builder
    .AddCratisArc(configureOptions: options =>
    {
        options.GeneratedApis.RoutePrefix = "api";
        options.GeneratedApis.IncludeCommandNameInRoute = false;
        options.GeneratedApis.SegmentsToSkipForRoute = 1;
    });

builder.Services.AddSingleton<IAuthenticationHandler, CookieAuthenticationHandler>();
builder.Services.AddSingleton<IAuthenticationHandler, MicrosoftIdentityPlatformHeaderAuthenticationHandler>();

var app = builder.Build();
app.UseCratisArc();

await app.RunAsync();
