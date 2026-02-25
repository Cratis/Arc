// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.for_IdentityProviderServiceCollectionExtensions.when_adding_identity_provider;

public class without_explicit_provider_and_multiple_discovered : given.a_service_collection_with_options
{
    Exception _exception;

    void Establish() => _types.FindMultiple<IProvideIdentityDetails>().Returns([typeof(FirstProvider), typeof(SecondProvider)]);

    void Because()
    {
        _services.AddIdentityProvider();
        using var serviceProvider = _services.BuildServiceProvider();
        _exception = Catch.Exception(() => serviceProvider.GetRequiredService<IProvideIdentityDetails>());
    }

    [Fact] void should_throw_multiple_identity_details_providers_found() => _exception.ShouldBeOfExactType<MultipleIdentityDetailsProvidersFound>();

    class FirstProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "First" }));
    }

    class SecondProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "Second" }));
    }
}
