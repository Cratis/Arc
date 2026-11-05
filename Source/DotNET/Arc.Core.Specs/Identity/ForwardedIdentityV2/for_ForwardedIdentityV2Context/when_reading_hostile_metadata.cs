// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Context;

public class when_reading_hostile_metadata : Specification
{
    readonly List<string> _failures = [];
    int _checked;

    async Task Because()
    {
        var baseline = ForwardedIdentityV2Vectors.Named("valid-v2");
        var deepActor = "{\"iss\":\"service\",\"sub\":\"actor\"}";
        for (var depth = 0; depth < 10; depth++) deepActor = "{\"iss\":\"service\",\"sub\":\"actor\",\"act\":" + deepActor + "}";
        var thumbprint = VerificationFixture.Url(new byte[32]);
        var values = new (string Header, string Value, ForwardedIdentityV2Rejection Reason)[]
        {
            ("x-cratis-amr", "not-base64", ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-amr", VerificationFixture.Base64("["), ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-amr", VerificationFixture.Base64("[null]"), ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-amr", VerificationFixture.Base64("[\" \" ]"), ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-amr", Convert.ToBase64String(new byte[] { 255 }), ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-act", VerificationFixture.Base64(deepActor), ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-act", VerificationFixture.Base64("{\"iss\":\"s\",\"sub\":\"a\",\"extra\":true}"), ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-policy-epoch", "+1", ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-policy-epoch", "01", ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-auth-time", "-0", ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-auth-time", "1.5", ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-sender-binding", VerificationFixture.Base64("{\"jkt\":\"" + thumbprint + "\",\"jti\":\"\"}"), ForwardedIdentityV2Rejection.Metadata),
            ("x-cratis-sender-binding", VerificationFixture.Base64("{\"jkt\":\"" + thumbprint + "\",\"jti\":\"proof\",\"extra\":true}"), ForwardedIdentityV2Rejection.Metadata),
            ("x-ms-client-principal", VerificationFixture.Base64("[]"), ForwardedIdentityV2Rejection.Identity),
            ("x-ms-client-principal", Convert.ToBase64String(new byte[] { 255 }), ForwardedIdentityV2Rejection.Identity),
            ("x-ms-client-principal", VerificationFixture.Base64("{\"userId\":\"not-uuid\",\"userRoles\":[1],\"claims\":[]}"), ForwardedIdentityV2Rejection.Identity),
            ("x-ms-client-principal-id", "UTF-8''%FF", ForwardedIdentityV2Rejection.Identity)
        };
        foreach (var value in values)
        {
            var vector = VerificationFixture.Header(baseline, value.Header, value.Value);
            var result = await VerificationFixture.Verifier(vector).Verify(VerificationFixture.Settings(vector.Inputs), VerificationFixture.Request(vector), CancellationToken.None);
            _checked++;
            if (result.Rejection != value.Reason) _failures.Add($"{value.Header}: expected {value.Reason}, got {result.Rejection}");
        }
    }

    [Fact] void should_exercise_every_hostile_control() => _checked.ShouldEqual(17);
    [Fact] void should_refuse_malformed_signed_metadata_and_identity() => _failures.ShouldBeEmpty();
}
