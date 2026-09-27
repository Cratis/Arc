// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_protecting_catalog_with_derived_header_as_default : Specification
{
    Exception? _failure;

    void Because() => _failure = given.catalog_guard.Map(builder => builder.Services.AddAuthentication("Header").AddScheme<AuthenticationSchemeOptions, given.derived_header_handler>("Header", _ => { }));

    [Fact] void should_refuse_unsigned_headers() => _failure.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
}
