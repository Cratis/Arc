// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_protecting_catalog_with_selector_and_no_registered_header : Specification
{
    Exception? _failure;

    void Because() => _failure = given.catalog_guard.Map(builder => builder.Services.AddAuthentication("Clean").AddScheme<AuthenticationSchemeOptions, given.catalog_authentication_handler>("Clean", options => options.ForwardDefaultSelector = _ => "Clean"));

    [Fact] void should_start_without_unsigned_headers() => _failure.ShouldBeNull();
}
