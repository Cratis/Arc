// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Samples;
using Samples.Authentication;
using Samples.Features.CallerKeyedLookups;
using Samples.Features.QueryShowcase;

var builder = ArcApplication.CreateBuilder(args);

_ = typeof(ShowcaseItem);

builder
    .AddCratisArc(configureOptions: options =>
    {
        options.AddJsonTypeInfoResolver(SamplesJsonSerializerContext.Default)
            .AddJsonTypeInfoResolver(CallerKeyedLookupsJsonSerializerContext.Default);
        options.GeneratedApis.RoutePrefix = "api";
        options.GeneratedApis.IncludeCommandNameInRoute = false;
        options.GeneratedApis.SegmentsToSkipForRoute = 1;
    });

// The cookie handler trusts unsigned client cookies; register it for local demos only.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IAuthenticationHandler, CookieAuthenticationHandler>();
}

builder.Services.AddSingleton<IAuthenticationHandler, MicrosoftIdentityPlatformHeaderAuthenticationHandler>();

var app = builder.Build();
app.UseCratisArc();

await app.RunAsync();
