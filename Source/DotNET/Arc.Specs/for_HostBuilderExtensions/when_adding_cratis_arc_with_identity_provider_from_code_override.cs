// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions;

public class when_adding_cratis_arc_with_identity_provider_from_code_override : Specification
{
    Type _resolvedProviderType;

    void Because()
    {
        using var host = new HostBuilder()
            .AddCratisArc(options => options.IdentityDetailsProvider = typeof(TestIdentityDetailsProvider))
            .Build();

        var provider = host.Services.GetRequiredService<IProvideIdentityDetails>();
        _resolvedProviderType = provider.GetType();
    }

    [Fact] void should_use_provider_from_code() => _resolvedProviderType.ShouldEqual(typeof(TestIdentityDetailsProvider));

    class TestIdentityDetailsProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "Test" }));
    }
}
