// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Concepts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

[Collection("UsesCurrentDirectory")]
public class when_a_string_concept_receives_a_json_number : Specification
{
    int _status;
    string _body;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(RegisterEmail));
        handler.Location.Returns(["ConceptBinding", nameof(RegisterEmail)]);
        handler.AllowsAnonymousAccess.Returns(true);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        builder.Services.AddSingleton(handlers);
        await using var app = builder.Build();
        var mapper = new AspNetCoreEndpointMapper(app);
        mapper.MapCommandEndpoints(app.Services);
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == $"Execute{typeof(RegisterEmail).FullName}");
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{ "email": 42 }"""));
        context.Response.Body = new MemoryStream();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        await endpoint.RequestDelegate!(context);
        _status = context.Response.StatusCode;
        _body = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
        TestContext.Current.TestOutputHelper?.WriteLine($"HTTP {_status}: {_body}");
    }

    [Fact] void should_respond_with_bad_request() => _status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_report_a_malformed_command_body() => JsonDocument.Parse(_body).RootElement.GetProperty("isValid").GetBoolean().ShouldBeFalse();

    public record Email(string Value) : ConceptAs<string>(Value);

    [AllowAnonymous]
    public record RegisterEmail(Email Email);
}
