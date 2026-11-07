// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>A session cookie alone is not a presentable credential.</summary>
public class with_session_cookie_only : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie"]);
        _raw.GetHeaderValues("Authorization").Returns([]);
        _raw.GetHeaderValues("Cookie").Returns(["cratis-identity-session=fixture"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_report_no_credential() => _result.Refusal.ShouldEqual(CallerRefusal.NoCredential);
}
