// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Verifier;

public class when_checks_fail : Specification
{
    readonly List<ForwardedIdentityV2Rejection?> _results = [];
    int _bearerCalls;
    int _storeCalls;
    ForwardedIdentityV2Rejection? _afterInvalidSignature;
    ForwardedIdentityV2Rejection? _afterInvalidMetadata;
    ForwardedIdentityV2Rejection? _afterInvalidBearer;
    ForwardedIdentityV2Rejection? _afterStoreException;

    async Task Because()
    {
        var baseline = ForwardedIdentityV2Vectors.Named("valid-v2");
        var bearers = new VectorBearerVerifier(null);
        var counter = new CountingStore();
        var verifier = VerificationFixture.Verifier(baseline, counter, bearers);
        foreach (var name in new[] { "signature-zero-scalars", "timestamp-stale", "timestamp-too-far-ahead", "known-kid-wrong-key-material", "tamper-x-ms-client-principal" })
        {
            var vector = ForwardedIdentityV2Vectors.Named(name);
            var result = await verifier.Verify(VerificationFixture.Settings(vector.Inputs), VerificationFixture.Request(vector), CancellationToken.None);
            _results.Add(result.Rejection);
        }
        var bad = baseline with { Envelope = baseline.Envelope[..^86] + new string('A', 86) };
        _results.Add((await verifier.Verify(VerificationFixture.Settings(baseline.Inputs), VerificationFixture.Request(bad), CancellationToken.None)).Rejection);
        _bearerCalls = bearers.Calls;
        _storeCalls = counter.Calls;
        _afterInvalidSignature = (await verifier.Verify(VerificationFixture.Settings(baseline.Inputs), VerificationFixture.Request(baseline), CancellationToken.None)).Rejection;
        var malformed = VerificationFixture.Header(baseline, "x-cratis-amr", VerificationFixture.Base64("[false]"));
        var metadataStore = new CountingStore();
        var metadataVerifier = VerificationFixture.Verifier(baseline, metadataStore);
        _results.Add((await metadataVerifier.Verify(VerificationFixture.Settings(baseline.Inputs), VerificationFixture.Request(malformed), CancellationToken.None)).Rejection);
        _afterInvalidMetadata = (await metadataVerifier.Verify(VerificationFixture.Settings(baseline.Inputs), VerificationFixture.Request(baseline), CancellationToken.None)).Rejection;
        var bearer = ForwardedIdentityV2Vectors.Named("matching-forwarded-bearer");
        var bindingStore = new ProcessLocalForwardedIdentityNonceStore();
        var refused = VerificationFixture.Verifier(bearer, bindingStore, new VectorBearerVerifier(null));
        _results.Add((await refused.Verify(VerificationFixture.Settings(bearer.Inputs), VerificationFixture.Request(bearer), CancellationToken.None)).Rejection);
        _afterInvalidBearer = (await VerificationFixture.Verifier(bearer, bindingStore).Verify(VerificationFixture.Settings(bearer.Inputs), VerificationFixture.Request(bearer), CancellationToken.None)).Rejection;
        _afterStoreException = (await VerificationFixture.Verifier(baseline, new ThrowingStore()).Verify(VerificationFixture.Settings(baseline.Inputs), VerificationFixture.Request(baseline), CancellationToken.None)).Rejection;
    }

    [Fact] void should_refuse_every_invalid_control() => _results.TrueForAll(reason => reason is not null).ShouldBeTrue();
    [Fact] void should_not_dispatch_a_bearer_before_signature_verification() => _bearerCalls.ShouldEqual(0);
    [Fact] void should_not_reserve_before_checks_pass() => _storeCalls.ShouldEqual(0);
    [Fact] void should_not_poison_nonce_on_bad_signature() => _afterInvalidSignature.ShouldBeNull();
    [Fact] void should_not_poison_nonce_on_bad_metadata() => _afterInvalidMetadata.ShouldBeNull();
    [Fact] void should_not_poison_nonce_on_refused_bearer() => _afterInvalidBearer.ShouldBeNull();
    [Fact] void should_fail_closed_if_store_throws() => _afterStoreException.ShouldEqual(ForwardedIdentityV2Rejection.Store);
}
