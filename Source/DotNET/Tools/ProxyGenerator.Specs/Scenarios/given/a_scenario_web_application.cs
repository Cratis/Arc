// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ControllerBased;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.ProxyGenerator.Scenarios.given;

/// <summary>
/// A reusable context that sets up the scenario web application with Arc infrastructure.
/// </summary>
public class a_scenario_web_application : Specification, IDisposable
{
    /// <summary>
    /// Gets or sets the web application factory.
    /// </summary>
    protected IHost? Host { get; set; }

    /// <summary>
    /// Gets or sets the HTTP client for making requests.
    /// </summary>
    protected HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    protected JavaScriptRuntime? Runtime { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript-HTTP bridge.
    /// </summary>
    protected JavaScriptHttpBridge? Bridge { get; set; }

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        // Suppress verbose logging during tests
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(a_scenario_web_application).Assembly);

        builder.Services.AddRouting();
        builder.Host.AddCratisArc(_ => { });

        var app = builder.Build();

        app.UseRouting();
        app.UseCratisArc();
        app.MapControllers();

        // Start the application
        Host = app;
        app.Start();

        HttpClient = app.GetTestClient();
        Runtime = new JavaScriptRuntime();
        Bridge = new JavaScriptHttpBridge(Runtime, HttpClient);
    }

    /// <summary>
    /// Generates and loads a command proxy for the given command type.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    protected void LoadCommandProxy<TCommand>()
    {
        var commandType = typeof(TCommand).GetTypeInfo();
        var descriptor = commandType.ToCommandDescriptor(
            targetPath: string.Empty,
            segmentsToSkip: 0,
            skipCommandNameInRoute: false,
            apiPrefix: "api");

        var code = InMemoryProxyGenerator.GenerateCommand(descriptor);
        Bridge.LoadTypeScript(code);
    }

    /// <summary>
    /// Generates and loads a controller-based command proxy for the given controller method.
    /// </summary>
    /// <typeparam name="TController">The controller type.</typeparam>
    /// <param name="methodName">The name of the controller method.</param>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the method is not found.</exception>
    protected void LoadControllerCommandProxy<TController>(string methodName)
    {
        var controllerType = typeof(TController).GetTypeInfo();
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on type '{controllerType.Name}'");

        var descriptor = method.ToCommandDescriptor(
            targetPath: string.Empty,
            segmentsToSkip: 0);

        var code = InMemoryProxyGenerator.GenerateCommand(descriptor);
        Bridge.LoadTypeScript(code);
    }

    /// <summary>
    /// Generates and loads a query proxy for the given read model type and query method.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="methodName">The name of the query method.</param>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the method is not found.</exception>
    protected void LoadQueryProxy<TReadModel>(string methodName)
    {
        var readModelType = typeof(TReadModel).GetTypeInfo();
        var method = readModelType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on type '{readModelType.Name}'");

        var descriptor = method.ToQueryDescriptor(
            readModelType,
            targetPath: string.Empty,
            segmentsToSkip: 0,
            skipQueryNameInRoute: false,
            apiPrefix: "api");

        var code = InMemoryProxyGenerator.GenerateQuery(descriptor);
        Bridge.LoadTypeScript(code);
    }

    /// <summary>
    /// Generates and loads a controller-based query proxy for the given controller method.
    /// </summary>
    /// <typeparam name="TController">The controller type.</typeparam>
    /// <param name="methodName">The name of the controller method.</param>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the method is not found.</exception>
    protected void LoadControllerQueryProxy<TController>(string methodName)
    {
        var controllerType = typeof(TController).GetTypeInfo();
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on type '{controllerType.Name}'");

        var descriptor = method.ToQueryDescriptor(
            targetPath: string.Empty,
            segmentsToSkip: 0);

        var code = InMemoryProxyGenerator.GenerateQuery(descriptor);
        Bridge.LoadTypeScript(code);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Bridge?.Dispose();
            HttpClient?.Dispose();
            Host?.Dispose();
        }
    }
}
