// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_malformed_authorization : given.a_gate
{
    CredentialPresentation[] _results;

    void Because()
    {
        string[] malformed = ["", "Bearer", "Basic fixture", "DPoP fixture", "Bearer  fixture", "Bearer\tfixture", "Bearer fixture ", "Bearer fixture-a,fixture-b", "Bearer fixture-a Bearer fixture-b", "Bearer =fixture", "Bearer a=b", "Bearer café"];
        _results = malformed.Select(value =>
        {
            _raw.GetHeaderValues("Authorization").Returns([value]);
            return _gate.Inspect(_request);
        }).ToArray();
    }

    [Fact] void should_refuse_every_malformed_bearer() => _results.All(result => result.Refusal == CallerRefusal.MalformedAuthorization).ShouldBeTrue();
    [Fact] void should_report_a_bad_request() => CallerRefusal.MalformedAuthorization.HttpStatus().ShouldEqual(400);
}
