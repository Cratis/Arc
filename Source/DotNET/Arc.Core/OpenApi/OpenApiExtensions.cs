// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods for OpenAPI support.
/// </summary>
public static class OpenApiExtensions
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maps an OpenAPI endpoint that serves the OpenAPI specification document.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/> to add the endpoint to.</param>
    /// <param name="pattern">The route pattern for the OpenAPI document. Defaults to "/openapi.json".</param>
    /// <param name="title">The title of the API. Defaults to "Arc Application".</param>
    /// <param name="version">The version of the API. Defaults to "1.0.0".</param>
    /// <returns>The <see cref="ArcApplication"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the endpoint mapper is not HttpListenerEndpointMapper.</exception>
    public static ArcApplication MapOpenApi(
        this ArcApplication app,
        string pattern = "/openapi.json",
        string title = "Arc Application",
        string version = "1.0.0")
    {
        if (app.EndpointMapper is not HttpListenerEndpointMapper httpListenerMapper)
        {
            throw new InvalidOperationException("MapOpenApi requires HttpListenerEndpointMapper");
        }

        app.EndpointMapper.MapGet(
            pattern,
            async context =>
            {
                var document = GenerateOpenApiDocument(httpListenerMapper, title, version);
                var json = JsonSerializer.Serialize(document, _jsonOptions);

                context.ContentType = "application/json";
                await context.WriteAsync(json);
            },
            new EndpointMetadata(
                Name: "OpenAPI",
                Summary: "OpenAPI specification document",
                Tags: ["OpenAPI"],
                AllowAnonymous: true));

        return app;
    }

    static Dictionary<string, object> GenerateOpenApiDocument(HttpListenerEndpointMapper mapper, string title, string version)
    {
        var routes = mapper.GetRoutes().ToList();
        var paths = new Dictionary<string, object>();

        foreach (var pathGroup in routes.GroupBy(r => r.Pattern))
        {
            var operations = new Dictionary<string, object>();

            foreach (var route in pathGroup)
            {
                var operation = new Dictionary<string, object>
                {
                    ["responses"] = new Dictionary<string, object>
                    {
                        ["200"] = new Dictionary<string, object>
                        {
                            ["description"] = "Success"
                        },
                        ["401"] = new Dictionary<string, object>
                        {
                            ["description"] = "Unauthorized"
                        },
                        ["500"] = new Dictionary<string, object>
                        {
                            ["description"] = "Internal Server Error"
                        }
                    }
                };

                if (!string.IsNullOrEmpty(route.Metadata?.Name))
                {
                    operation["operationId"] = route.Metadata.Name;
                }

                if (!string.IsNullOrEmpty(route.Metadata?.Summary))
                {
                    operation["summary"] = route.Metadata.Summary;
                }

                if (route.Metadata?.Tags is not null && route.Metadata.Tags.Any())
                {
                    operation["tags"] = route.Metadata.Tags.ToArray();
                }

                if (route.Metadata?.AllowAnonymous == false)
                {
                    operation["security"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["Bearer"] = Array.Empty<string>()
                        }
                    };
                }

                operations[route.Method.ToLowerInvariant()] = operation;
            }

            paths[pathGroup.Key] = operations;
        }

        var document = new Dictionary<string, object>
        {
            ["openapi"] = "3.0.0",
            ["info"] = new Dictionary<string, object>
            {
                ["title"] = title,
                ["version"] = version
            },
            ["servers"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["url"] = "/"
                }
            },
            ["paths"] = paths
        };

        if (routes.Exists(r => r.Metadata?.AllowAnonymous == false))
        {
            document["components"] = new Dictionary<string, object>
            {
                ["securitySchemes"] = new Dictionary<string, object>
                {
                    ["Bearer"] = new Dictionary<string, object>
                    {
                        ["type"] = "http",
                        ["scheme"] = "bearer",
                        ["bearerFormat"] = "JWT",
                        ["description"] = "JWT Authorization header using the Bearer scheme."
                    }
                }
            };
        }

        return document;
    }
}
