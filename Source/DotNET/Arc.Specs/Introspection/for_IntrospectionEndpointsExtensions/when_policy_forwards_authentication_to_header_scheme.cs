// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_policy_forwards_authentication_to_header_scheme : Specification
{
    Exception? _failure;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthentication("Forwarded")
            .AddPolicyScheme("Forwarded", null, options => options.ForwardAuthenticate = "MicrosoftIdentityPlatform")
            .AddScheme<AuthenticationSchemeOptions, Identity.MicrosoftIDentityPlatformAuthHandler>("MicrosoftIdentityPlatform", _ => { });
        builder.Services.AddAuthorization();
        builder.AddCratisArc(options => options.Introspection.RequireAuthentication = true);
        using var app = builder.Build();
        _failure = Catch.Exception(() => app.MapIntrospectionEndpoints());
    }

    [Fact] void should_refuse_forwarded_unsigned_headers() => _failure.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
}
