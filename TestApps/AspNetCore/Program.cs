// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();
builder.Services.AddControllers();
builder.Services.AddMvc();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddConcepts());

var app = builder.Build();
app.UseRouting();
app.UseWebSockets();
app.UseCratisArc();

app.MapControllers();
app.MapGet("/", () => "Hello World!");

app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();
