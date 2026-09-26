// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc;
using Cratis.Concepts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_minimal_api_handles_concept_collections_and_keys : Specification
{
    WebApplication? _app;
    string _collectionJson;
    string _dictionaryJson;
    IEnumerable<AccountId>? _collection;
    Dictionary<AccountId, string>? _dictionary;
    readonly AccountId _id = new(Guid.NewGuid());

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _app = builder.Build();
        var arcOptions = _app.Services.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions;
        _collectionJson = JsonSerializer.Serialize((IEnumerable<AccountId>)[_id], arcOptions);
        _dictionaryJson = JsonSerializer.Serialize(new Dictionary<AccountId, string> { [_id] = "value" }, arcOptions);

        var collectionContext = new DefaultHttpContext { RequestServices = _app.Services };
        collectionContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(_collectionJson));
        collectionContext.Request.ContentType = "application/json";
        _collection = await collectionContext.Request.ReadFromJsonAsync<IEnumerable<AccountId>>();

        var dictionaryContext = new DefaultHttpContext { RequestServices = _app.Services };
        dictionaryContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(_dictionaryJson));
        dictionaryContext.Request.ContentType = "application/json";
        _dictionary = await dictionaryContext.Request.ReadFromJsonAsync<Dictionary<AccountId, string>>();

        collectionContext.Response.Body = new MemoryStream();
        await Results.Ok((IEnumerable<AccountId>)[_id]).ExecuteAsync(collectionContext);
        collectionContext.Response.Body.Position = 0;
        _collectionJson = await new StreamReader(collectionContext.Response.Body, Encoding.UTF8).ReadToEndAsync();

        dictionaryContext.Response.Body = new MemoryStream();
        await Results.Ok(new Dictionary<AccountId, string> { [_id] = "value" }).ExecuteAsync(dictionaryContext);
        dictionaryContext.Response.Body.Position = 0;
        _dictionaryJson = await new StreamReader(dictionaryContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_bind_a_collection_of_concepts() => _collection.ShouldEqual([_id]);
    [Fact] void should_bind_a_dictionary_with_concept_keys() => _dictionary![_id].ShouldEqual("value");
    [Fact] void should_write_a_collection_of_primitive_values() => _collectionJson.ShouldEqual($"[\"{_id.Value}\"]");
    [Fact] void should_write_concept_dictionary_keys_like_arc_endpoints() => JsonNode.DeepEquals(JsonNode.Parse(_dictionaryJson), JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<AccountId, string> { [_id] = "value" }, _app!.Services.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions))).ShouldBeTrue();

    public record AccountId(Guid Value) : ConceptAs<Guid>(Value);
}
