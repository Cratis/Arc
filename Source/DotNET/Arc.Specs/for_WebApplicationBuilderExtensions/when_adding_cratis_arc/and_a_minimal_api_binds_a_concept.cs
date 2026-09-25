// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Concepts;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_a_minimal_api_binds_a_concept : Specification
{
    WebApplication _app;
    AccountId _result;
    Guid _id;

    void Establish() => _id = Guid.NewGuid();

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        _app = builder.Build();
        var options = _app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
        _result = JsonSerializer.Deserialize<AccountId>($"\"{_id}\"", options);
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_deserialize_the_primitive_as_a_concept() => _result.ShouldEqual(new AccountId(_id));

    public record AccountId(Guid Value) : ConceptAs<Guid>(Value);
}
