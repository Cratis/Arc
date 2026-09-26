// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Arc;
using Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;
using Cratis.Concepts;
using Cratis.Geospatial;
using Cratis.Json;
using Cratis.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_explicit_arc_converters : Specification
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
    [Fact] void should_describe_exactly_the_declared_polymorphic_properties() => ((JsonObject)Schema("ISampleBase")["properties"]!).Select(property => property.Key).Order().ToArray().ShouldEqual(["hidden", "id", "name"]);
    [Fact] void should_describe_declared_polymorphic_properties_as_strings() => ((JsonObject)Schema("ISampleBase")["properties"]!).All(property => property.Value?["type"]?.ToString() == "string").ShouldBeTrue();
    [Fact] void should_serialize_derived_properties_beyond_the_base_schema() => _wire!.AsObject().Select(property => property.Key).Order().ToArray().ShouldEqual(["count", "hidden", "id", "name"]);
    [Fact] void should_not_write_a_discriminator_for_the_runtime_typed_minimal_api_response() => _wire!["_derivedTypeId"].ShouldBeNull();
    [Fact] void should_serialize_the_derived_property() => _wire!["count"]!.ToString().ShouldEqual("42");
    [Fact] void should_describe_the_complex_key_dictionary_as_an_object() => Schema("DictionaryOfSampleConceptAndstring")["type"]!.ToString().ShouldEqual("object");
    [Fact] void should_describe_the_complex_key_dictionary_values() => Schema("DictionaryOfSampleConceptAndstring")["additionalProperties"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_describe_controller_response_using_the_same_options() => _document!["paths"]!["/controller"]!["get"]!["responses"]!["200"]!["content"]!["text/json"]!["schema"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/IEnumerableOfSampleConcept");

    JsonNode Schema(string name) => _document!["components"]!["schemas"]![name]!;

    static async Task<(JsonNode? Document, JsonNode? Wire)> GenerateDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();

        // Non-concept Arc converters are opt-in for plain minimal APIs.
        builder.Services.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>().PostConfigure<IOptions<ArcOptions>>((options, arc) =>
        {
            foreach (var converter in arc.Value.JsonSerializerOptions.Converters.Where(converter =>
                converter is DerivedTypeJsonConverterFactory or DateOnlyJsonConverter or TimeOnlyJsonConverter or TypeJsonConverter or UriJsonConverter or PointJsonConverter or LineStringJsonConverter or PolygonJsonConverter))
            {
                options.SerializerOptions.Converters.Add(converter);
            }
        });
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
