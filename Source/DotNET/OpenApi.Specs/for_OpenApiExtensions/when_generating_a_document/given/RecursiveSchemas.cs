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
    public static Task<JsonNode?> Generate(bool polymorphic) => Task.Run(() => GenerateDocument(polymorphic)).WaitAsync(TimeSpan.FromSeconds(10));

    public static Task<JsonNode?> GenerateDictionary<TValue>() => Task.Run(() => GenerateDictionaryDocument<TValue>()).WaitAsync(TimeSpan.FromSeconds(10));

    public static Task<JsonNode?> GeneratePolymorphicCollection(bool nested) => Task.Run(() => GeneratePolymorphicCollectionDocument(nested)).WaitAsync(TimeSpan.FromSeconds(10));

    static async Task<JsonNode?> GeneratePolymorphicCollectionDocument(bool nested)
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOpenApi(options => options.AddConcepts());
        await using var app = builder.Build();
        if (nested)
        {
            app.MapGet("/nodes", () => TypedResults.Ok<IListNode>(new ListNode([])));
        }
        else
        {
            app.MapGet("/nodes", () => TypedResults.Ok<IEnumerable<IListNode>>([]));
        }
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        return JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    static async Task<JsonNode?> GenerateDictionaryDocument<TValue>()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOpenApi(options => options.AddConcepts());
        await using var app = builder.Build();
        app.MapGet("/dictionary", () => TypedResults.Ok(new Dictionary<SomeConcept, TValue>()));
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        return JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    static async Task<JsonNode?> GenerateDocument(bool polymorphic)
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
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        return JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    public static bool ReferencesResolve(JsonNode document) => References(document).All(reference =>
        reference.StartsWith("#/components/schemas/", StringComparison.Ordinal) &&
        document["components"]?["schemas"]?[reference["#/components/schemas/".Length..]] is not null);

    static IEnumerable<string> References(JsonNode? node)
    {
        if (node is JsonObject properties)
        {
            foreach (var property in properties)
            {
                if (property.Key == "$ref" && property.Value is not null)
                {
                    yield return property.Value.ToString();
                }
                else
                {
                    foreach (var reference in References(property.Value))
                    {
                        yield return reference;
                    }
                }
            }
        }
        else if (node is JsonArray items)
        {
            foreach (var item in items)
            {
                foreach (var reference in References(item))
                {
                    yield return reference;
                }
            }
        }
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
    public record Folder(IEnumerable<Folder> Children);
    public record ListNodeValue(List<ListNodeValue> Children);
#pragma warning disable CA1819 // The array is part of the schema under specification.
    public record ArrayNodeValue(ArrayNodeValue[] Children);
#pragma warning restore CA1819
    public record ReadOnlyNodeValue(IReadOnlyList<ReadOnlyNodeValue> Children);
    public record NullableNode(NullableNode? Next, int Count);

    public interface IListNode
    {
        IReadOnlyList<IListNode> Children { get; }
    }

    [DerivedType("listNode", typeof(IListNode))]
    public record ListNode(IReadOnlyList<IListNode> Children) : IListNode;
}
