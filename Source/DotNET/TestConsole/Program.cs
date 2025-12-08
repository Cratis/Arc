// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = ArcApplicationBuilder.CreateBuilder(args);

builder.AddCratisArc(arcBuilder =>
{
    // Configure Arc here if needed
});

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

app.UseCratisArc("http://*:5001/");

Console.WriteLine("Test Console App starting...");
Console.WriteLine("Listening on http://*:5001/");
Console.WriteLine();
Console.WriteLine("Available endpoints:");
Console.WriteLine("  POST   http://*:5001/api/test-console/say-hello-command");
Console.WriteLine("  GET    http://*:5001/api/test-console/get-greeting-query?name=World");
Console.WriteLine("  GET    http://*:5001/api/test-console/stream-updates (WebSocket)");
Console.WriteLine("  GET    http://*:5001/.cratis/me");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop...");

await app.RunAsync();
