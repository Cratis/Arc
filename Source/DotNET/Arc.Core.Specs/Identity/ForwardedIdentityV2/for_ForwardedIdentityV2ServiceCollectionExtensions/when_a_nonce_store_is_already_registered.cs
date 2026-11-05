// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2ServiceCollectionExtensions;

public class when_a_nonce_store_is_already_registered : Specification
{
    readonly IForwardedIdentityNonceStore _registered = new CountingStore();
    IForwardedIdentityNonceStore _resolved;
    int _registrations;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_registered);
        services.AddProcessLocalForwardedIdentityNonceStore();
        services.AddForwardedIdentityV2(VerificationFixture.Settings(ForwardedIdentityV2Vectors.Named("valid-v2").Inputs));
        using var provider = services.BuildServiceProvider();
        _resolved = provider.GetRequiredService<IForwardedIdentityNonceStore>();
        _registrations = services.Count(descriptor => descriptor.ServiceType == typeof(IForwardedIdentityNonceStore));
    }

    [Fact] void should_accept_and_preserve_an_explicit_store() => ReferenceEquals(_resolved, _registered).ShouldBeTrue();
    [Fact] void should_not_add_an_implicit_alternative() => _registrations.ShouldEqual(1);
}
