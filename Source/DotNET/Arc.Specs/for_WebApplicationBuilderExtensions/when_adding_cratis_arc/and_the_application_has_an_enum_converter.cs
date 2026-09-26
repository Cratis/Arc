// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_the_application_has_an_enum_converter : Specification
{
    WebApplication? _app;
    string _json;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.AddCratisArc();
        _app = builder.Build();
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Response.Body = new MemoryStream();
        await Results.Ok(Choice.Second).ExecuteAsync(context);
        context.Response.Body.Position = 0;
        _json = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_use_the_application_converter_first() => _json.ShouldEqual("\"Second\"");

    public enum Choice
    {
        First = 0,
        Second = 1
    }
}
