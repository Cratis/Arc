// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Concepts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

public static class ConceptArrays
{
    public static async Task<JsonNode?> GenerateDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOpenApi(options => options.AddConcepts());
        await using var app = builder.Build();
        app.MapGet("/keys", () => TypedResults.Ok(Array.Empty<Key>()));
        app.MapGet("/nested-keys", () => TypedResults.Ok(new KeyContainer([])));
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        return JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    public record Key(string Value) : ConceptAs<string>(Value);
#pragma warning disable CA1819 // The array is part of the schema under specification.
    public record KeyContainer(Key[] Keys);
#pragma warning restore CA1819
}
