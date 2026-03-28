// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Swagger;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Uncomment the following line to configure the application to use invariant culture
// builder.UseInvariantCulture();
builder.AddCratisArc();
builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
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
