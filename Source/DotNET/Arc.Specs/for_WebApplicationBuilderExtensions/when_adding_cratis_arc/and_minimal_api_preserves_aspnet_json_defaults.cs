// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_minimal_api_preserves_aspnet_json_defaults : Specification
{
    WebApplication? _app;
    string _json;
    LongChoice? _bound;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _app = builder.Build();
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("9223372036854775807"));
        _bound = await context.Request.ReadFromJsonAsync<LongChoice>();
        context.Response.Body = new MemoryStream();
        await Results.Ok(new Sample(LongChoice.Large, new TimeOnly(12, 34, 56, 123, 456))).ExecuteAsync(context);
        context.Response.Body.Position = 0;
        _json = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_bind_long_backed_enums_beyond_int_max_value() => _bound.ShouldEqual(LongChoice.Large);
    [Fact] void should_write_the_aspnet_numeric_enum_and_time_only_format() => _json.ShouldEqual("{\"choice\":9223372036854775807,\"time\":\"12:34:56.1234560\"}");

    public record Sample(LongChoice Choice, TimeOnly Time);
#pragma warning disable CA1028 // A long-backed enum beyond int.MaxValue is the regression case.
    public enum LongChoice : long
    {
        Large = long.MaxValue
    }
#pragma warning restore CA1028
}
