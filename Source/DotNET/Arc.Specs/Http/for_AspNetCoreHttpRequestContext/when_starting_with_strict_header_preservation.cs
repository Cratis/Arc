// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext;

public class when_starting_with_strict_header_preservation : Specification
{
    Exception _error;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(options => options.Tenancy.RequireDuplicateHeaderPreservation = true, configureBuilder: arc => arc.WithoutControllers());
        await using var app = builder.Build();
        app.UseCratisArc();
        _error = await Catch.Exception(() => app.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact] void should_allow_the_host_that_preserves_repeated_headers() => _error.ShouldBeNull();
}
