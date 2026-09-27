// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_plain_minimal_api_json_defaults : Specification
{
    JsonNode? _document;
    JsonNode? _response;
    JsonNode? _stringDocument;
    JsonNode? _stringResponse;

    async Task Because()
    {
        (_document, _response) = await Generate(false);
        (_stringDocument, _stringResponse) = await Generate(true);
    }

    [Fact] void should_describe_the_long_enum_as_numeric() => _document!["components"]!["schemas"]!["LongChoice"]!["type"]!.ToString().ShouldEqual("integer");
    [Fact] void should_describe_the_full_long_enum_value() => _document!["components"]!["schemas"]!["LongChoice"]!["enum"]!.ToJsonString().ShouldEqual("[9223372036854775807]");
    [Fact] void should_serialize_the_long_enum_and_time_with_aspnet_defaults() => _response!.ToJsonString().ShouldEqual("{\"choice\":9223372036854775807,\"time\":\"12:34:56.1234560\"}");
    [Fact] void should_describe_the_string_enum_with_string_values() => _stringDocument!["components"]!["schemas"]!["LongChoice"]!["type"]!.ToString().ShouldEqual("string");
    [Fact] void should_match_the_string_enum_schema_to_the_actual_response() => _stringDocument!["components"]!["schemas"]!["LongChoice"]!["enum"]![0]!.ToString().ShouldEqual(_stringResponse!["choice"]!.ToString());
    [Fact] void should_serialize_the_string_enum_response() => _stringResponse!["choice"]!.ToString().ShouldEqual("Large");

    static async Task<(JsonNode? Document, JsonNode? Response)> Generate(bool stringEnum)
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        if (stringEnum)
        {
            builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        }
        builder.Services.AddOpenApi(options => options.AddConcepts());
        await using var app = builder.Build();
        app.MapGet("/sample", () => TypedResults.Ok(new Sample(LongChoice.Large, new TimeOnly(12, 34, 56, 123, 456))));
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Response.Body = new MemoryStream();
        await Results.Ok(new Sample(LongChoice.Large, new TimeOnly(12, 34, 56, 123, 456))).ExecuteAsync(context);
        context.Response.Body.Position = 0;
        var wire = JsonNode.Parse(await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync());
        return (JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1)), wire);
    }

    public record Sample(LongChoice Choice, TimeOnly Time);
#pragma warning disable CA1028 // The long-backed enum is the regression case.
    public enum LongChoice : long
    {
        Large = long.MaxValue
    }
#pragma warning restore CA1028
}
