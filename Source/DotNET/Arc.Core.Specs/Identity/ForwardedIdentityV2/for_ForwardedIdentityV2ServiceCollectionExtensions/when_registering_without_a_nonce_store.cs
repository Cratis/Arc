// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2ServiceCollectionExtensions;

public class when_registering_without_a_nonce_store : Specification
{
    Exception _failure;
    readonly ServiceCollection _services = new();

    void Because()
    {
        try
        {
            _services.AddForwardedIdentityV2(VerificationFixture.Settings(ForwardedIdentityV2Vectors.Named("valid-v2").Inputs));
        }
        catch (ForwardedIdentityNonceStoreNotConfigured error)
        {
            _failure = error;
        }
    }

    [Fact] void should_require_an_explicit_nonce_store() => _failure.ShouldBeOfExactType<ForwardedIdentityNonceStoreNotConfigured>();
    [Fact] void should_explain_the_missing_registration_neutrally() => _failure.Message.ShouldEqual("Forwarded identity v2 requires an explicitly registered nonce store.");
    [Fact] void should_not_partially_register_the_profile() => _services.ShouldBeEmpty();
}
