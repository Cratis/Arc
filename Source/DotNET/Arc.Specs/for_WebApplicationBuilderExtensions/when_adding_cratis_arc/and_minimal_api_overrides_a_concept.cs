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
    string _json;
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
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_bind_an_object_instead_of_a_primitive() => _bound.ShouldEqual(_id);
    [Fact] void should_serialize_an_object_instead_of_a_primitive() => _json.ShouldEqual($"{{\"value\":\"{_id.Value}\"}}");

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
