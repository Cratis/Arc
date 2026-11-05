// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_repeated_envelope_headers : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["X-CRATIS-FORWARDED-IDENTITY-SIGNATURE"]);
        _raw.GetHeaderValues("Authorization").Returns([]);
        _raw.GetHeaderValues("X-CRATIS-FORWARDED-IDENTITY-SIGNATURE").Returns(["1", "1"]);
    }
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_duplicate_envelope_fields() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_report_a_bad_request() => CallerRefusal.MultipleCredentials.HttpStatus().ShouldEqual(400);
}
