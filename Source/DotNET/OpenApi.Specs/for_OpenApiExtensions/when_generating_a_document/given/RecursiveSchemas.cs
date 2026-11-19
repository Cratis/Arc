// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc;
using Cratis.Concepts;
using Cratis.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

public static class RecursiveSchemas
{
    public static Task<JsonNode?> Generate(bool polymorphic) => GenerateDocument(app =>
    {
        if (polymorphic)
        {
            app.MapGet("/node", () => TypedResults.Ok<INode>(new DerivedNode(null, [])));
        }
        else
        {
            app.MapGet("/node", () => TypedResults.Ok(new Node([])));
        }
    });

    public static Task<JsonNode?> GenerateDictionary<TValue>() =>
        GenerateDocument(app => app.MapGet("/dictionary", () => TypedResults.Ok(new Dictionary<SomeConcept, TValue>())));

    public static Task<JsonNode?> GeneratePolymorphicCollection(bool nested) => GenerateDocument(app =>
    {
        if (nested)
        {
            app.MapGet("/nodes", () => TypedResults.Ok<IListNode>(new ListNode([])));
        }
        else
        {
            app.MapGet("/nodes", () => TypedResults.Ok<IEnumerable<IListNode>>([]));
        }
    });

    static async Task<JsonNode?> GenerateDocument(Action<WebApplication> mapEndpoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOpenApi(options => options.AddConcepts());
        ConfigureDerivedTypes(builder);
        await using var app = builder.Build();
        mapEndpoints(app);
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");

        // Guard synchronous schema recursion without timing host startup under parallel load.
        var document = await Task.Run(() => provider.GetOpenApiDocumentAsync(CancellationToken.None)).WaitAsync(TimeSpan.FromSeconds(30));
        return JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    static void ConfigureDerivedTypes(WebApplicationBuilder builder) =>
        builder.Services.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>().PostConfigure<IOptions<ArcOptions>>((options, arc) =>
            options.SerializerOptions.Converters.Add(arc.Value.JsonSerializerOptions.Converters.OfType<DerivedTypeJsonConverterFactory>().Single()));

    public static bool ReferencesResolve(JsonNode document) => References(document).All(reference =>
        reference.StartsWith("#/components/schemas/", StringComparison.Ordinal) &&
        document["components"]?["schemas"]?[reference["#/components/schemas/".Length..]] is not null);

    static IEnumerable<string> References(JsonNode? node) => node switch
    {
        JsonObject properties => properties.SelectMany(References),
        JsonArray items => items.SelectMany(References),
        _ => []
    };

    static IEnumerable<string> References(KeyValuePair<string, JsonNode?> property) =>
        property.Key == "$ref" && property.Value is not null
            ? [property.Value.ToString()]
            : References(property.Value);

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
