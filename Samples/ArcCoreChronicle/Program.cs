// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authentication;
using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;
using Samples;
using Samples.Authentication;
using Samples.Features.ProtectedProfiles;

// A ProjectReference does not import the package build files that turn read protection on, so declare it here.
[assembly: ReadProtection(ReadProtectionMode.On)]

var builder = ArcApplication.CreateBuilder(args);

builder
    .AddCratisArc(
        configureOptions: options =>
        {
            options.AddJsonTypeInfoResolver(SamplesJsonSerializerContext.Default)
                .AddJsonTypeInfoResolver(ProtectedProfilesJsonSerializerContext.Default);
            options.GeneratedApis.RoutePrefix = "api";
            options.GeneratedApis.IncludeCommandNameInRoute = false;
            options.GeneratedApis.SegmentsToSkipForRoute = 1;
        },

        // The kernel address comes from Cratis:Chronicle:ConnectionString and defaults to the development
        // connection string (localhost:35000). The client connects in the background once the host has started.
        configureBuilder: arc => arc.WithChronicle(options => options.EventStore = "ArcCoreChronicleAcceptance"));

// CookieAuthenticationHandler is discovered automatically and only authenticates in the Development environment.
builder.Services.AddSingleton<IAuthenticationHandler, MicrosoftIdentityPlatformHeaderAuthenticationHandler>();

var app = builder.Build();
app.UseCratisArc();

await app.RunAsync();
