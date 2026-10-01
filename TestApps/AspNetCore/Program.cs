// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Uncomment the following line to configure the application to use invariant culture
// builder.UseInvariantCulture();

// The browser sets the x-ms-client-principal headers itself to simulate a trusted ingress (EasyAuth or AuthProxy),
// so this local test app opts in to trusting them. Never do this when clients can reach the app directly.
builder.AddCratisArc(options => options.TrustForwardedIdentityHeaders = true);
builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
builder.Services.AddCratisMongoDB();
builder.Services.AddControllers();
builder.Services.AddMvc();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddConcepts());

var app = builder.Build();

// If using invariant culture, also apply the middleware:
// app.UseInvariantCulture();
app.UseRouting();
app.UseWebSockets();
app.UseCratisArc();

app.MapControllers();
app.MapGet("/", () => "Hello World!");

app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();
