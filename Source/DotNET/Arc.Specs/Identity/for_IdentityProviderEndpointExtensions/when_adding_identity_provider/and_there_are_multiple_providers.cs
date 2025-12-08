// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.for_IdentityProviderEndpointExtensions.when_adding_identity_provider;

public class and_there_are_multiple_providers : Specification
{
    IServiceCollection _services;
    ITypes _types;
    Exception _exception;

    void Establish()
    {
        _services = Substitute.For<IServiceCollection>();
        _types = Substitute.For<ITypes>();
        _types.FindMultiple<IProvideIdentityDetails>().Returns([typeof(object), typeof(object)]);
    }

    void Because() => _exception = Catch.Exception(() => _services.AddIdentityProvider(_types));

    [Fact] void should_throw_multiple_identity_details_providers_found() => _exception.ShouldBeOfExactType<MultipleIdentityDetailsProvidersFound>();
    [Fact] void should_not_add_service_registration() => _services.DidNotReceive().Add(Arg.Any<ServiceDescriptor>());
}
