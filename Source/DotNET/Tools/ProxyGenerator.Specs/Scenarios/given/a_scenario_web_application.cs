// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Arc.ProxyGenerator.ControllerBased;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.ProxyGenerator.Scenarios.given;

/// <summary>
/// A reusable context that sets up the scenario web application with Arc infrastructure.
/// </summary>
public class a_scenario_web_application : Specification, IDisposable
{
    readonly HashSet<Type> _loadedTypes = [];

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

    /// <summary>
    /// Gets the base URL of the running server.
    /// </summary>
    protected string? ServerUrl { get; set; }

    async Task Establish()
    {
        // Reset static test data for test isolation - must happen BEFORE test runs
        for_ObservableQueries.ModelBound.ObservableReadModel.Reset();
        for_ObservableQueries.ModelBound.ParameterizedObservableReadModel.Reset();
        for_ObservableQueries.ModelBound.ComplexObservableReadModel.Reset();

        var builder = WebApplication.CreateBuilder();

        // Use Kestrel instead of TestServer to support real WebSocket connections
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2));

        // Enable logging for debugging
        builder.Logging.ClearProviders();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(a_scenario_web_application).Assembly);

        builder.Services.AddRouting();
        builder.AddCratisArc(_ => { });

        // Register controller state as singleton for test isolation
        builder.Services.AddSingleton<for_ObservableQueries.ControllerBased.ObservableControllerQueriesState>();

        var app = builder.Build();

        // Add exception handling for better error visibility
        app.UseDeveloperExceptionPage();

        app.UseWebSockets();
        app.UseRouting();
        app.UseCratisArc();
        app.MapControllers();

        // Start the application
        Host = app;
        await app.StartAsync();

        // Get the actual server address
        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerUrl = addresses?.Addresses.FirstOrDefault() ?? "http://localhost:5000";

        // Create HTTP client pointing to the real server
        HttpClient = new HttpClient { BaseAddress = new Uri(ServerUrl) };

        Runtime = new JavaScriptRuntime();
        Bridge = new JavaScriptHttpBridge(Runtime, HttpClient, ServerUrl);
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
    /// <param name="saveToFile">Optional file path to save the generated TypeScript code.</param>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the method is not found.</exception>
    protected void LoadControllerCommandProxy<TController>(string methodName, string? saveToFile = null)
    {
        var controllerType = typeof(TController).GetTypeInfo();
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on type '{controllerType.Name}'");

        var descriptor = method.ToCommandDescriptor(
            targetPath: string.Empty,
            segmentsToSkip: 0);

        var code = InMemoryProxyGenerator.GenerateCommand(descriptor);

        if (!string.IsNullOrEmpty(saveToFile))
        {
            File.WriteAllText(saveToFile, code);
        }

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
        // Load types twice to ensure dependencies are available
        // First pass loads types but some may have missing dependencies
        // Second pass ensures all dependencies from first pass are now available
        LoadTypesAndEnumsPass(types, saveBasePath);
        LoadTypesAndEnumsPass(types, saveBasePath);
    }

    void LoadTypesAndEnumsPass(IEnumerable<Type> types, string? saveBasePath = null)
    {
        var typesList = types.Distinct().Where(t => !_loadedTypes.Contains(t)).ToList();
        var enums = typesList.Where(_ => _.IsEnum).ToList();
        var nonEnums = typesList.Where(_ => !_.IsEnum).ToList();

        if (typesList.Count == 0)
        {
            return;
        }

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
            _loadedTypes.Add(enumType);
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
            _loadedTypes.Add(type);
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

        // Load all types and enums that are involved
        LoadTypesAndEnums(descriptor.TypesInvolved);

        var code = InMemoryProxyGenerator.GenerateQuery(descriptor);
        Bridge.LoadTypeScript(code);
        Bridge.Runtime.Execute($"var __testQuery = new {descriptor.Name}();");
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
