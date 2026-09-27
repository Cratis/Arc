// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

public class and_minimal_api_naming_policy_was_configured : Specification
{
    WebApplication _app;
    JsonOptions _options;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = null);
        builder.AddCratisArc();
        _app = builder.Build();
        _options = _app.Services.GetRequiredService<IOptions<JsonOptions>>().Value;
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_preserve_the_application_naming_policy() => _options.SerializerOptions.PropertyNamingPolicy.ShouldBeNull();
}
