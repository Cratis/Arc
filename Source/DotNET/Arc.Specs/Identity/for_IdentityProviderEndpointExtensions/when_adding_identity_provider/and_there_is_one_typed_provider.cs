// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.for_IdentityProviderEndpointExtensions.when_adding_identity_provider;

public class and_there_is_one_typed_provider : Specification
{
    record MyDetails(string Name, int Age);

    class MyTypedIdentityProvider : IProvideIdentityDetails<MyDetails>
    {
        public Task<IdentityDetails<MyDetails>> ProvideDetails(IdentityProviderContext context)
            => Task.FromResult(new IdentityDetails<MyDetails>(true, new MyDetails("John", 30)));
    }

    IServiceCollection _services;
    ITypes _types;
    List<ServiceDescriptor> _serviceDescriptors;
    ServiceDescriptor _serviceDescriptor;

    void Establish()
    {
        _serviceDescriptors = [];
        _services = Substitute.For<IServiceCollection>();
        _types = Substitute.For<ITypes>();
        _types.FindMultiple<IProvideIdentityDetails>().Returns([typeof(MyTypedIdentityProvider)]);
        _services
            .When(_ => _.Add(Arg.Any<ServiceDescriptor>()))
            .Do(_ => _serviceDescriptors.Add(_.Arg<ServiceDescriptor>()));
    }

    void Because()
    {
        _services.AddIdentityProvider(_types);
        _serviceDescriptor = _serviceDescriptors.Find(_ => _.ImplementationType == typeof(MyTypedIdentityProvider));
    }

    [Fact] void should_register_expected_provider() => _serviceDescriptor.ShouldNotBeNull();
    [Fact] void should_register_as_identity_details_provider() => _serviceDescriptor.ServiceType.ShouldEqual(typeof(IProvideIdentityDetails));
    [Fact] void should_register_as_transient() => _serviceDescriptor.Lifetime.ShouldEqual(ServiceLifetime.Transient);
}
