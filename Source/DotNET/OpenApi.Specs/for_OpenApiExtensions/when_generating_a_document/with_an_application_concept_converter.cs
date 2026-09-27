// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Concepts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document;

public class with_an_application_concept_converter : Specification
{
    JsonNode? _document;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new AccountIdObjectConverter()));
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOpenApi(options => options.AddConcepts());
        await using var app = builder.Build();
        app.MapGet("/account", () => TypedResults.Ok(new AccountId(Guid.NewGuid())));
        app.MapGet("/accounts", () => TypedResults.Ok((IEnumerable<AccountId>)[new AccountId(Guid.NewGuid())]));
        await app.StartAsync();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        _document = JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1));
    }

    [Fact] void should_not_invent_a_primitive_schema_for_the_application_converter() => _document!["components"]!["schemas"]!["AccountId"]!["type"].ShouldBeNull();
    [Fact] void should_describe_collection_items_using_the_same_concept_schema() => _document!["components"]!["schemas"]!["IEnumerableOfAccountId"]!["items"]!["$ref"]!.ToString().ShouldEqual("#/components/schemas/AccountId");

    public record AccountId(Guid Value) : ConceptAs<Guid>(Value);

    public class AccountIdObjectConverter : JsonConverter<AccountId>
    {
        public override AccountId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return new AccountId(document.RootElement.GetProperty("value").GetGuid());
        }

        public override void Write(Utf8JsonWriter writer, AccountId value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("value", value.Value);
            writer.WriteEndObject();
        }
    }
}
