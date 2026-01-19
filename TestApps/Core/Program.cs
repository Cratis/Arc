// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;

var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc(configureOptions: options =>
{
    options.GeneratedApis.RoutePrefix = "api";
    options.GeneratedApis.IncludeCommandNameInRoute = false;
    options.GeneratedApis.SegmentsToSkipForRoute = 1;
});

var app = builder.Build();
app.UseCratisArc();

await app.RunAsync();