// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Verifier;

public class when_the_hop_is_not_authenticated : Specification
{
    ForwardedIdentityV2Rejection? _refusal;
    ForwardedIdentityV2Rejection? _retry;
    readonly CountingStore _store = new();
    readonly VectorBearerVerifier _bearers = new(null);
    int _callsBeforeRetry;

    async Task Because()
    {
        var vector = ForwardedIdentityV2Vectors.Named("valid-v2");
        var verifier = VerificationFixture.Verifier(vector, _store, _bearers);
        var untrusted = vector with { Inputs = vector.Inputs with { AuthenticatedTls = false } };
        _refusal = (await verifier.Verify(VerificationFixture.Settings(vector.Inputs), VerificationFixture.Request(untrusted), CancellationToken.None)).Rejection;
        _callsBeforeRetry = _store.Calls;
        _retry = (await verifier.Verify(VerificationFixture.Settings(vector.Inputs), VerificationFixture.Request(vector), CancellationToken.None)).Rejection;
    }

    [Fact] void should_refuse_the_hop() => _refusal.ShouldEqual(ForwardedIdentityV2Rejection.Hop);
    [Fact] void should_not_reserve_a_nonce() => _callsBeforeRetry.ShouldEqual(0);
    [Fact] void should_not_call_a_bearer_verifier() => _bearers.Calls.ShouldEqual(0);
    [Fact] void should_accept_the_same_nonce_after_admission() => _retry.ShouldBeNull();
}
