// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Concepts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_minimal_api_overrides_a_concept : Specification
{
    WebApplication? _app;
    AccountId? _bound;
    IEnumerable<AccountId>? _boundCollection;
    Dictionary<AccountId, string>? _boundDictionary;
    string _json;
    string _collectionJson;
    string _dictionaryJson;
    readonly AccountId _id = new(Guid.NewGuid());

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new AccountIdObjectConverter()));
        _app = builder.Build();

        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"{{\"value\":\"{_id.Value}\"}}"));
        _bound = await context.Request.ReadFromJsonAsync<AccountId>();
        context.Response.Body = new MemoryStream();
        await Results.Ok(_id).ExecuteAsync(context);
        context.Response.Body.Position = 0;
        _json = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        var collectionContext = new DefaultHttpContext { RequestServices = _app.Services };
        collectionContext.Request.ContentType = "application/json";
        collectionContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"[{{\"value\":\"{_id.Value}\"}}]"));
        _boundCollection = await collectionContext.Request.ReadFromJsonAsync<IEnumerable<AccountId>>();
        collectionContext.Response.Body = new MemoryStream();
        await Results.Ok((IEnumerable<AccountId>)[_id]).ExecuteAsync(collectionContext);
        collectionContext.Response.Body.Position = 0;
        _collectionJson = await new StreamReader(collectionContext.Response.Body, Encoding.UTF8).ReadToEndAsync();

        var dictionaryContext = new DefaultHttpContext { RequestServices = _app.Services };
        dictionaryContext.Request.ContentType = "application/json";
        dictionaryContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"{{\"account-{_id.Value}\":\"name\"}}"));
        _boundDictionary = await dictionaryContext.Request.ReadFromJsonAsync<Dictionary<AccountId, string>>();
        dictionaryContext.Response.Body = new MemoryStream();
        await Results.Ok(new Dictionary<AccountId, string> { [_id] = "name" }).ExecuteAsync(dictionaryContext);
        dictionaryContext.Response.Body.Position = 0;
        _dictionaryJson = await new StreamReader(dictionaryContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_bind_an_object_instead_of_a_primitive() => _bound.ShouldEqual(_id);
    [Fact] void should_serialize_an_object_instead_of_a_primitive() => _json.ShouldEqual($"{{\"value\":\"{_id.Value}\"}}");
    [Fact] void should_bind_collection_items_with_the_application_converter() => _boundCollection.ShouldEqual([_id]);
    [Fact] void should_write_collection_items_with_the_application_converter() => _collectionJson.ShouldEqual($"[{{\"value\":\"{_id.Value}\"}}]");
    [Fact] void should_bind_dictionary_keys_with_the_application_converter() => _boundDictionary![_id].ShouldEqual("name");
    [Fact] void should_write_dictionary_keys_with_the_application_converter() => _dictionaryJson.ShouldEqual($"{{\"account-{_id.Value}\":\"name\"}}");

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

        public override AccountId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(Guid.Parse(reader.GetString()!["account-".Length..]));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, AccountId value, JsonSerializerOptions options) =>
            writer.WritePropertyName($"account-{value.Value}");
    }
}
