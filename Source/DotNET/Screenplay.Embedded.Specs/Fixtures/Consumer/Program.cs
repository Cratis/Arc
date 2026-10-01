// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Company.Library;

/// <summary>
/// Runs the embedded event-model integration host.
/// </summary>
public static class Program
{
    /// <summary>
    /// Starts the fixture host with the embedded explorer enabled.
    /// </summary>
    /// <param name="args">The host arguments.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.MapCratisEventModel(typeof(Program).Assembly);
        app.Run();
    }
}
