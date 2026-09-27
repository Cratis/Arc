// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Concepts;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_a_minimal_api_binds_a_concept : Specification
{
    WebApplication? _app;
    AccountId? _result;
    readonly Guid _id = Guid.NewGuid();

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _app = builder.Build();
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"\"{_id}\""));
        context.Request.ContentType = "application/json";
        _result = await context.Request.ReadFromJsonAsync<AccountId>();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_deserialize_the_primitive_as_a_concept() => _result.ShouldEqual(new AccountId(_id));

    public record AccountId(Guid Value) : ConceptAs<Guid>(Value);
}
