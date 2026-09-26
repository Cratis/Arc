// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_mapping_directly_with_unsigned_header_scheme : Specification
{
    Exception? _failure;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
        builder.Services.AddAuthorization();
        builder.AddCratisArc();
        using var app = builder.Build();
        var mapper = new AspNetCoreEndpointMapper(app);
        _failure = Catch.Exception(() => mapper.MapIntrospectionEndpoints(new IntrospectionOptions { RequireAuthentication = true, TrustForwardedIdentityHeaders = false }));
    }

    [Fact] void should_refuse_unsigned_identity_headers() => _failure.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
}
