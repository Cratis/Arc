// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

[Collection("UsesCurrentDirectory")]
public class when_trusting_forwarded_headers : Specification
{
    Exception? _failure;

    void Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc(options =>
        {
            options.Introspection.RequireAuthentication = true;
            options.Introspection.TrustForwardedIdentityHeaders = true;
        });
        using var app = builder.Build();
        _failure = Catch.Exception(() => app.UseCratisArc());
    }

    [Fact] void should_allow_mapping_behind_trusted_ingress() => _failure.ShouldBeNull();
}
