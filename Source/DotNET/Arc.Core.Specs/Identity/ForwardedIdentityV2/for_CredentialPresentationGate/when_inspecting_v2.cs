// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_CredentialPresentationGate;

public class when_inspecting_v2 : Specification
{
    CredentialPresentation _v2;
    CredentialPresentation _malformed;
    CredentialPresentation _v1;
    CredentialPresentation _duplicateAuthorization;
    CredentialPresentation _duplicateEnvelope;
    CredentialPresentation _badAuthorization;
    CredentialPresentation _unknownVersion;
    CredentialPresentation _dpop;

    void Because()
    {
        var vector = ForwardedIdentityV2Vectors.Named("matching-forwarded-bearer");
        var gate = new CredentialPresentationGate();
        _v2 = gate.Inspect(VerificationFixture.RawRequest(vector));
        _malformed = gate.Inspect(VerificationFixture.RawRequest(vector with { Envelope = "v2.malformed" }));
        _v1 = gate.Inspect(VerificationFixture.RawRequest(vector with { Envelope = "v1.synthetic.signature" }));
        _unknownVersion = gate.Inspect(VerificationFixture.RawRequest(vector with { Envelope = "v3.synthetic.signature" }));
        _duplicateAuthorization = gate.Inspect(VerificationFixture.RawRequest(vector with { Inputs = vector.Inputs with { Headers = vector.Inputs.Headers.Append(new Header("authorization", "Bearer duplicate")).ToArray() } }));
        _duplicateEnvelope = gate.Inspect(VerificationFixture.RawRequest(vector with { Inputs = vector.Inputs with { Headers = vector.Inputs.Headers.Append(new Header(ForwardedIdentityV2Envelope.Header, vector.Envelope)).ToArray() } }));
        _dpop = gate.Inspect(VerificationFixture.RawRequest(VerificationFixture.Header(vector, "Authorization", "DPoP fixture", false)));
        _badAuthorization = gate.Inspect(VerificationFixture.RawRequest(VerificationFixture.Header(vector, "Authorization", "Bearer space token", false)));
    }

    [Fact] void should_admit_v2_with_bearer_for_verification() => _v2.Refusal.ShouldBeNull();
    [Fact] void should_route_v2_with_bearer_to_the_envelope_verifier() => _v2.Shape.ShouldEqual(PresentedCredentialShape.Envelope);
    [Fact] void should_leave_malformed_v2_for_verification() => _malformed.Refusal.ShouldBeNull();
    [Fact] void should_route_malformed_v2_to_the_envelope_verifier() => _malformed.Shape.ShouldEqual(PresentedCredentialShape.Envelope);
    [Fact] void should_still_refuse_v1_with_bearer() => _v1.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_still_refuse_unknown_version_with_bearer() => _unknownVersion.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_refuse_duplicate_authorization() => _duplicateAuthorization.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_refuse_duplicate_envelope() => _duplicateEnvelope.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_preserve_exactly_one_authorization_value() => _v2.Authorization.Count.ShouldEqual(1);
    [Fact] void should_leave_authorization_syntax_to_the_v2_verifier() => _badAuthorization.Shape.ShouldEqual(PresentedCredentialShape.Envelope);
    [Fact] void should_route_dpop_to_the_v2_verifier_for_refusal() => _dpop.Shape.ShouldEqual(PresentedCredentialShape.Envelope);
}
