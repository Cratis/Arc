// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Chronicle;
using Cratis.Arc;

var builder = WebApplication.CreateBuilder(args);
builder.UseInvariantCulture();
builder.AddCratis(
    configureArcOptions: options =>
    {
        options.GeneratedApis.RoutePrefix = "api";
        options.GeneratedApis.IncludeCommandNameInRoute = true;
        options.GeneratedApis.SegmentsToSkipForRoute = 1;
    },
    configureChronicleBuilder: chronicleBuilder =>
    {
    });

var services = builder.Services.Where(s => s.ServiceType == typeof(MyReadModel)).ToList();

var app = builder.Build();

app.UseRouting();
app.UseWebSockets();
app.UseCratisArc();

app.MapControllers();
app.MapGet("/", () => "Hello World!");

app.UseCratis();

await app.RunAsync();