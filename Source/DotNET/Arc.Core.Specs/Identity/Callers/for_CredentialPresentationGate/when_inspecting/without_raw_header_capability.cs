// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class without_raw_header_capability : given.a_gate
{
    void Establish()
    {
        _request = Substitute.For<IHttpRequestContext>();
        _request.Headers.Returns(new Dictionary<string, string> { { "Authorization", "Bearer fixture" } });
    }
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_not_fall_back_to_flattened_headers() => _result.Refusal.ShouldEqual(CallerRefusal.UnreadableCredentialHeaders);
}
