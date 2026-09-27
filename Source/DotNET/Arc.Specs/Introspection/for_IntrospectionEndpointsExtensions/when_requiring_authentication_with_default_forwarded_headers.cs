// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_requiring_authentication_with_default_forwarded_headers : Specification
{
    Exception? _failure;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
        builder.Services.AddAuthorization();
        builder.AddCratisArc(options => options.Introspection.RequireAuthentication = true);
        using var app = builder.Build();
        _failure = Catch.Exception(() => app.MapIntrospectionEndpoints());
    }

    [Fact] void should_refuse_unsigned_identity_headers_at_startup() => _failure.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
}
