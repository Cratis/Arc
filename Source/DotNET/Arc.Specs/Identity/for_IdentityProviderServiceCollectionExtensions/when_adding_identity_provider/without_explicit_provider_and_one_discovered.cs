// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.for_IdentityProviderServiceCollectionExtensions.when_adding_identity_provider;

public class without_explicit_provider_and_one_discovered : given.a_service_collection_with_options
{
    IProvideIdentityDetails _provider;

    void Establish() => _types.FindMultiple<IProvideIdentityDetails>().Returns([typeof(MyIdentityProvider)]);

    void Because()
    {
        _services.AddIdentityProvider();
        using var serviceProvider = _services.BuildServiceProvider();
        _provider = serviceProvider.GetRequiredService<IProvideIdentityDetails>();
    }

    [Fact] void should_use_discovered_provider() => _provider.ShouldBeOfExactType<MyIdentityProvider>();

    class MyIdentityProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "Discovered" }));
    }
}
