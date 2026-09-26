// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;
using Cratis.Concepts;
using Cratis.Geospatial;
using Cratis.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_arc_json_defaults : Specification
{
    JsonNode? _document;
    JsonNode? _wire;

    async Task Because() => (_document, _wire) = await GenerateDocument();

    [Fact] void should_describe_the_concept_as_a_primitive() => Schema("SampleConcept")["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_the_concept_collection_as_an_array() => Schema("IEnumerableOfSampleConcept")["type"]!.ToString().ShouldEqual("array");
    [Fact] void should_describe_the_concept_collection_items() => Schema("IEnumerableOfSampleConcept")["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/SampleConcept");
    [Fact] void should_preserve_the_enum_type() => Schema("SampleEnum")["type"]!.ToString().ShouldEqual("integer");
    [Fact] void should_describe_numeric_enum_values() => Schema("SampleEnum")["enum"]!.ToJsonString().ShouldEqual("[0,1]");
    [Fact] void should_describe_dates_as_strings() => Schema("SampleDates")["properties"]!["date"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_nullable_dates_as_strings_or_null() => Schema("SampleDates")["properties"]!["nullableDate"]!["type"]!.ToString().ShouldContain("null");
    [Fact] void should_describe_times_as_strings() => Schema("SampleDates")["properties"]!["time"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_nullable_times_as_strings_or_null() => Schema("SampleDates")["properties"]!["nullableTime"]!["type"]!.ToString().ShouldContain("null");
    [Fact] void should_describe_uris_as_strings() => Schema("SampleDates")["properties"]!["uri"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_nullable_uris_as_strings_or_null() => Schema("SampleDates")["properties"]!["nullableUri"]!["type"]!.ToString().ShouldContain("null");
    [Fact] void should_describe_types_as_strings() => Schema("Type")["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_geojson_points_as_objects() => Schema("Point")["properties"]!["coordinates"]!["items"]!["type"]!.ToString().ShouldEqual("number");
    [Fact] void should_describe_geojson_lines_as_objects() => Schema("LineString")["properties"]!["coordinates"]!["items"]!["items"]!["type"]!.ToString().ShouldEqual("number");
    [Fact] void should_describe_geojson_polygons_as_objects() => Schema("Polygon")["properties"]!["coordinates"]!["items"]!["items"]!["items"]!["type"]!.ToString().ShouldEqual("number");
    [Fact] void should_describe_primitive_collections_as_arrays() => Schema("SampleValues")["properties"]!["names"]!["type"]!.ToString().ShouldEqual("array");
    [Fact] void should_describe_declared_polymorphic_properties() => Schema("ISampleBase")["properties"]!["name"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_inherited_interface_properties_as_they_are_written() => Schema("ISampleBase")["properties"]!["id"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_include_ignored_interface_properties_that_are_written() => Schema("ISampleBase")["properties"]!["hidden"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_not_apply_interface_json_property_names() => Schema("ISampleBase")["properties"]!["renamed"].ShouldBeNull();
    [Fact] void should_describe_only_properties_present_on_the_wire() => ((JsonObject)Schema("ISampleBase")["properties"]!).All(property => _wire!.AsObject().ContainsKey(property.Key)).ShouldBeTrue();
    [Fact] void should_serialize_ignored_and_renamed_interface_properties_by_clr_name() => _wire!["id"]!.ToString().ShouldEqual("id");
    [Fact] void should_serialize_the_hidden_property() => _wire!["hidden"]!.ToString().ShouldEqual("hidden");
    [Fact] void should_not_serialize_the_interface_json_property_name() => _wire!["renamed"].ShouldBeNull();
    [Fact] void should_exclude_static_properties() => Schema("ISampleBase")["properties"]!["staticValue"].ShouldBeNull();
    [Fact] void should_describe_the_complex_key_dictionary_as_an_object() => Schema("DictionaryOfSampleConceptAndstring")["type"]!.ToString().ShouldEqual("object");
    [Fact] void should_describe_the_complex_key_dictionary_values() => Schema("DictionaryOfSampleConceptAndstring")["additionalProperties"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_controller_response_using_the_same_options() => _document!["paths"]!["/controller"]!["get"]!["responses"]!["200"]!["content"]!["text/json"]!["schema"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/IEnumerableOfSampleConcept");

    JsonNode Schema(string name) => _document!["components"]!["schemas"]![name]!;

    static async Task<(JsonNode? Document, JsonNode? Wire)> GenerateDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOpenApi(options => options.AddConcepts());
        builder.Services.AddControllers().AddApplicationPart(typeof(SchemaController).Assembly);
        await using var app = builder.Build();
        app.MapControllers();
        app.MapGet("/concept", () => TypedResults.Ok(new SampleConcept(Guid.NewGuid())));
        app.MapGet("/concepts", () => TypedResults.Ok((IEnumerable<SampleConcept>)[new SampleConcept(Guid.NewGuid())]));
        app.MapGet("/enum", () => TypedResults.Ok(SampleEnum.Second));
        app.MapGet("/derived", () => TypedResults.Ok<ISampleBase>(new SampleDerived("value", 42)));
        app.MapGet("/dictionary", () => TypedResults.Ok(new Dictionary<SampleConcept, string>()));
        app.MapGet("/dates", () => TypedResults.Ok(new SampleDates(DateOnly.MinValue, null, TimeOnly.MinValue, null, new Uri("https://example.com"), null)));
        app.MapGet("/values", () => TypedResults.Ok(new SampleValues(typeof(string), new Point(1, 2), null, null, ["a"])));
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Response.Body = new MemoryStream();
        await Results.Ok<ISampleBase>(new SampleDerived("value", 42)).ExecuteAsync(context);
        context.Response.Body.Position = 0;
        var wire = JsonNode.Parse(await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync());
        return (JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1)), wire);
    }

    public record SampleConcept(Guid Value) : ConceptAs<Guid>(Value);
    public record SampleDates(DateOnly Date, DateOnly? NullableDate, TimeOnly Time, TimeOnly? NullableTime, Uri Uri, Uri? NullableUri);
    public record SampleValues(Type RuntimeType, Point Point, LineString? Line, Polygon? Polygon, IEnumerable<string> Names);
    public enum SampleEnum
    {
        First = 0,
        Second = 1
    }
    public interface IAdditionalProperties
    {
        [JsonPropertyName("renamed")]
        string Id { get; }
        [JsonIgnore]
        string Hidden { get; }
        static string StaticValue => "static";
    }
    public interface ISampleBase : IAdditionalProperties
    {
        string Name { get; }
    }

    [DerivedType("sample-derived", typeof(ISampleBase))]
    public record SampleDerived(string Name, int Count) : ISampleBase
    {
        public string Id => "id";
        public string Hidden => "hidden";
    }
}
