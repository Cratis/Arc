// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_installing_with_a_fleet_nonce_store : given.an_installation
{
    ServiceProvider _provider;
    void Establish() => _builder.ForResource("https://resource.example", "audience", "test").UsePersonDirectory<PersonDirectory>()
        .UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer("https://issuer.example").AddPublicKey("test", _publicKey).UseNonceStore<FleetNonceStore>());
    void Because()
    {
        _services.AddVerifiedCallers(_builder);
        _provider = _services.BuildServiceProvider();
    }
    void Destroy() => _provider.Dispose();
    [Fact] void should_use_the_selected_store() => _provider.GetRequiredService<IForwardedIdentityNonceStore>().ShouldBeOfExactType<FleetNonceStore>();

    public class FleetNonceStore : IForwardedIdentityNonceStore
    {
        public Task<ForwardedIdentityNonceReservation> Reserve(string aud, string kid, string nonce, long timestamp, long now, CancellationToken cancellationToken) => Task.FromResult(ForwardedIdentityNonceReservation.Unavailable);
    }
}
