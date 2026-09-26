// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_minimal_api_options_are_configured_after_arc : Specification
{
    WebApplication? _app;
    JsonOptions _options;
    string _json;
    JsonStringEnumConverter _converter;
    ConceptAsJsonConverterFactory _arcConverter;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _converter = new JsonStringEnumConverter();
        _arcConverter = new ConceptAsJsonConverterFactory();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Insert(0, _converter);
            options.SerializerOptions.Converters.Add(_arcConverter);
        });
        _app = builder.Build();
        _options = _app.Services.GetRequiredService<IOptions<JsonOptions>>().Value;
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Response.Body = new MemoryStream();
        await Results.Ok(new { URL = "url", Choice = Choice.Second }).ExecuteAsync(context);
        context.Response.Body.Position = 0;
        _json = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_preserve_explicit_camel_case() => _options.SerializerOptions.PropertyNamingPolicy.ShouldEqual(JsonNamingPolicy.CamelCase);
    [Fact] void should_preserve_application_converter_order() => _options.SerializerOptions.Converters[0].ShouldEqual(_converter);
    [Fact] void should_not_duplicate_arc_converters() => _options.SerializerOptions.Converters.OfType<ConceptAsJsonConverterFactory>().Single().ShouldEqual(_arcConverter);
    [Fact] void should_preserve_application_wire_format() => _json.ShouldEqual("{\"url\":\"url\",\"choice\":\"Second\"}");

    public enum Choice
    {
        First = 0,
        Second = 1
    }
}
