// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.for_IdentityProviderEndpointExtensions.when_adding_identity_provider;

public class and_there_are_no_providers : Specification
{
    IServiceCollection _services;
    ITypes _types;
    List<ServiceDescriptor> _serviceDescriptors;

    void Establish()
    {
        _serviceDescriptors = [];
        _services = Substitute.For<IServiceCollection>();
        _types = Substitute.For<ITypes>();
        _services
            .When(_ => _.Add(Arg.Any<ServiceDescriptor>()))
            .Do(_ => _serviceDescriptors.Add(_.Arg<ServiceDescriptor>()));
    }

    void Because() => _services.AddIdentityProvider(_types);

    [Fact] void should_add_default_identity_details_provider() => _serviceDescriptors.Exists(_ => _.ImplementationType == typeof(DefaultIdentityDetailsProvider));
}