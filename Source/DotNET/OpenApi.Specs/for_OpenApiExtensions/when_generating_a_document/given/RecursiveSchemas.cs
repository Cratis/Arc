// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Concepts;
using Cratis.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

public static class RecursiveSchemas
{
    public static async Task<JsonNode?> Generate(bool polymorphic)
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOpenApi(options => options.AddConcepts());
        await using var app = builder.Build();
        if (polymorphic)
        {
            app.MapGet("/node", () => TypedResults.Ok<INode>(new DerivedNode(null, [])));
        }
        else
        {
            app.MapGet("/node", () => TypedResults.Ok(new Node([])));
        }

        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        return JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    public interface INode
    {
        INode? Parent { get; }
        IEnumerable<INode> Children { get; }
    }

    [DerivedType("node", typeof(INode))]
    public record DerivedNode(INode? Parent, IEnumerable<INode> Children) : INode;

    public record SomeConcept(string Value) : ConceptAs<string>(Value);
    public record Node(Dictionary<SomeConcept, Node> Children);
}
