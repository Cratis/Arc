// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;
using Cratis.Concepts;
using Cratis.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_arc_json_defaults : Specification
{
    JsonNode? _document;

    async Task Because() => _document = await GenerateDocument();

    [Fact] void should_describe_the_concept_as_a_primitive() => Schema("SampleConcept")["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_the_concept_collection_as_an_array() => Schema("IEnumerableOfSampleConcept")["type"]!.ToString().ShouldEqual("array");
    [Fact] void should_describe_the_concept_collection_items() => Schema("IEnumerableOfSampleConcept")["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/SampleConcept");
    [Fact] void should_preserve_the_enum_type() => Schema("SampleEnum")["type"]!.ToString().ShouldEqual("integer");
    [Fact] void should_describe_declared_polymorphic_properties() => Schema("ISampleBase")["properties"]!["name"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_the_complex_key_dictionary_as_an_object() => Schema("DictionaryOfSampleConceptAndstring")["type"]!.ToString().ShouldEqual("object");
    [Fact] void should_describe_the_complex_key_dictionary_values() => Schema("DictionaryOfSampleConceptAndstring")["additionalProperties"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_controller_response_using_the_same_options() => _document!["paths"]!["/controller"]!["get"]!["responses"]!["200"]!["content"]!["text/json"]!["schema"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/IEnumerableOfSampleConcept");

    JsonNode Schema(string name) => _document!["components"]!["schemas"]![name]!;

    static async Task<JsonNode?> GenerateDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddOpenApi(options => options.AddConcepts());
        builder.Services.AddControllers().AddApplicationPart(typeof(SchemaController).Assembly);
        await using var app = builder.Build();
        app.MapControllers();
        app.MapGet("/concept", () => TypedResults.Ok(new SampleConcept(Guid.NewGuid())));
        app.MapGet("/concepts", () => TypedResults.Ok((IEnumerable<SampleConcept>)[new SampleConcept(Guid.NewGuid())]));
        app.MapGet("/enum", () => TypedResults.Ok(SampleEnum.Second));
        app.MapGet("/derived", () => TypedResults.Ok<ISampleBase>(new SampleDerived("value", 42)));
        app.MapGet("/dictionary", () => TypedResults.Ok(new Dictionary<SampleConcept, string>()));
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        return JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    public record SampleConcept(Guid Value) : ConceptAs<Guid>(Value);
    public enum SampleEnum
    {
        First = 0,
        Second = 1
    }
    public interface ISampleBase
    {
        string Name { get; }
    }

    [DerivedType("sample-derived", typeof(ISampleBase))]
    public record SampleDerived(string Name, int Count) : ISampleBase;
}
