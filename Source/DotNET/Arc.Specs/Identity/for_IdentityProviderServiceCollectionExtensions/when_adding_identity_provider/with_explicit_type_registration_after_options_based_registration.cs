// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.for_IdentityProviderServiceCollectionExtensions.when_adding_identity_provider;

public class with_explicit_type_registration_after_options_based_registration : given.a_service_collection_with_options
{
    IProvideIdentityDetails _provider;

    void Establish()
    {
        _types.FindMultiple<IProvideIdentityDetails>().Returns([typeof(DiscoveredIdentityProvider)]);
    }

    void Because()
    {
        _services.AddIdentityProvider();
        _services.AddIdentityProvider<ExplicitIdentityProvider>();

        using var serviceProvider = _services.BuildServiceProvider();
        _provider = serviceProvider.GetRequiredService<IProvideIdentityDetails>();
    }

    [Fact] void should_use_explicitly_registered_provider() => _provider.ShouldBeOfExactType<ExplicitIdentityProvider>();

    class DiscoveredIdentityProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "Discovered" }));
    }

    class ExplicitIdentityProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "Explicit" }));
    }
}
