// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ControllerBased;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
        File.WriteAllText("/tmp/establish-called.txt", "Establish() called at " + DateTime.Now);

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

        // Debug: List all registered endpoints
        if (app is IEndpointRouteBuilder routeBuilder)
        {
            var endpoints = routeBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToArray();
            File.WriteAllText("/tmp/registered-endpoints.txt",
                string.Join("\n", endpoints.Select(e =>
                    e is RouteEndpoint re ? re.RoutePattern.RawText : e.DisplayName)));
        }

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
    /// <param name="saveToFile">Optional file path to save the generated TypeScript code.</param>
    protected void LoadCommandProxy<TCommand>(string? saveToFile = null)
    {
        var commandType = typeof(TCommand).GetTypeInfo();
        var descriptor = commandType.ToCommandDescriptor(
            targetPath: string.Empty,
            segmentsToSkip: 0,
            skipCommandNameInRoute: false,
            apiPrefix: "api");

        // Load all types and enums that are involved
        LoadTypesAndEnums(descriptor.TypesInvolved, saveToFile);

        var code = InMemoryProxyGenerator.GenerateCommand(descriptor);

        if (!string.IsNullOrEmpty(saveToFile))
        {
            File.WriteAllText(saveToFile, code);
        }

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
    /// <param name="saveToFile">Optional file path to save the generated TypeScript code.</param>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the method is not found.</exception>
    protected void LoadQueryProxy<TReadModel>(string methodName, string? saveToFile = null)
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

        // Load all types and enums that are involved
        LoadTypesAndEnums(descriptor.TypesInvolved, saveToFile);

        var code = InMemoryProxyGenerator.GenerateQuery(descriptor);

        if (!string.IsNullOrEmpty(saveToFile))
        {
            File.WriteAllText(saveToFile, code);
        }

        // For Observable queries, don't wrap in module scope to avoid prototype chain issues
        var isObservable = descriptor.IsObservable;
        
        File.WriteAllText("/tmp/isobservable.txt", $"IsObservable={isObservable}, wrapInModuleScope={!isObservable}");
        
        if (Bridge is null)
        {
            File.WriteAllText("/tmp/bridge-null.txt", "Bridge is null!");
            throw new InvalidOperationException("Bridge is null!");
        }

        File.WriteAllText("/tmp/calling-loadtypescript.txt", $"Calling LoadTypeScript, wrapInModuleScope={!isObservable}");

        Bridge.LoadTypeScript(code, wrapInModuleScope: !isObservable);

        File.WriteAllText("/tmp/loadtypescript-returned.txt", "LoadTypeScript returned");
    }

    /// <summary>
    /// Loads all types and enums into the JavaScript runtime.
    /// </summary>
    /// <param name="types">The types to load.</param>
    /// <param name="saveBasePath">Optional base path for saving generated files.</param>
    protected void LoadTypesAndEnums(IEnumerable<Type> types, string? saveBasePath = null)
    {
        var typesList = types.Distinct().ToList();
        var enums = typesList.Where(_ => _.IsEnum).ToList();
        var nonEnums = typesList.Where(_ => !_.IsEnum).ToList();

        var baseDir = !string.IsNullOrEmpty(saveBasePath) ? Path.GetDirectoryName(saveBasePath) : null;

        // Generate and load enum files
        foreach (var enumType in enums)
        {
            var enumDescriptor = enumType.ToEnumDescriptor();
            var enumCode = InMemoryProxyGenerator.GenerateEnum(enumDescriptor);

            if (!string.IsNullOrEmpty(baseDir))
            {
                var enumFile = Path.Combine(baseDir, $"{enumDescriptor.Name}.ts");
                File.WriteAllText(enumFile, enumCode);
            }

            Bridge.LoadTypeScript(enumCode);
        }

        // Generate and load type files
        foreach (var type in nonEnums)
        {
            var typeDescriptor = type.ToTypeDescriptor(string.Empty, 0);
            var typeCode = InMemoryProxyGenerator.GenerateType(typeDescriptor);

            if (!string.IsNullOrEmpty(baseDir))
            {
                var typeFile = Path.Combine(baseDir, $"{typeDescriptor.Name}.ts");
                File.WriteAllText(typeFile, typeCode);
            }

            Bridge.LoadTypeScript(typeCode);
        }
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
