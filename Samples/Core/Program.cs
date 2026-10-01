// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Http;

var builder = ArcApplication.CreateBuilder(args);

// Uncomment the following line to configure the application to use invariant culture
// builder.UseInvariantCulture();
builder.AddCratisArc(configureOptions: options =>
{
    options.GeneratedApis.RoutePrefix = "api";
    options.GeneratedApis.IncludeCommandNameInRoute = false;
    options.GeneratedApis.SegmentsToSkipForRoute = 1;
});

var app = builder.Build();
app.UseCratisArc();

app.MapGet("/", async (req) => await req.Write("Hello, World!"));

app.MapGet("/add-cookies", (req) =>
{
    req.AppendCookie("cookie1", "example-value", new CookieOptions { Path = "/" });
    req.AppendCookie("cookie2", "example-value", new CookieOptions { Path = "/" });
    req.AppendCookie("cookie3", "example-value", new CookieOptions { Path = "/" });
    return Task.CompletedTask;
});

app.MapGet("/remove-cookies", (req) =>
{
    req.RemoveCookie("cookie1");
    req.RemoveCookie("cookie2");
    req.RemoveCookie("cookie3");
    return Task.CompletedTask;
});

await app.RunAsync();