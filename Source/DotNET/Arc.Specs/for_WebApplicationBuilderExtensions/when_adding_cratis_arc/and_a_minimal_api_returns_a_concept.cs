// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Concepts;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_a_minimal_api_returns_a_concept : Specification
{
    WebApplication _app;
    string _json;
    static readonly Guid _id = Guid.NewGuid();

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _app = builder.Build();
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Response.Body = new MemoryStream();
        await Results.Ok(new { AccountId = new AccountId(_id) }).ExecuteAsync(context);
        context.Response.Body.Position = 0;
        _json = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_serialize_the_concept_as_its_primitive() => _json.ShouldEqual($"{{\"accountId\":\"{_id}\"}}");

    public record AccountId(Guid Value) : ConceptAs<Guid>(Value);
}
