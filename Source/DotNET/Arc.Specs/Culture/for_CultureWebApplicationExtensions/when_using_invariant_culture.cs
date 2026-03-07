// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Arc.Culture.for_CultureWebApplicationExtensions;

public class when_using_invariant_culture : Specification
{
    WebApplication? _app;
    WebApplicationBuilder _builder;

    void Establish()
    {
        _builder = WebApplication.CreateBuilder();
        _builder.UseInvariantCulture();
    }

    void Because()
    {
        _app = _builder.Build();
        _app.UseInvariantCulture();
    }

    [Fact] void should_return_web_application_for_continuation() =>
        _app.ShouldNotBeNull();

    void Destroy()
    {
        _app?.DisposeAsync().GetAwaiter().GetResult();
    }
}
