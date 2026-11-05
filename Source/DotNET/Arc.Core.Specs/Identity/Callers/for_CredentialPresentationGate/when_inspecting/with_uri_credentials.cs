// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Vector 23.</summary>
public class with_uri_credentials : given.a_gate
{
    CredentialPresentation[] _results;

    void Because()
    {
        string[] targets = ["/mcp?access_token=fixture", "/mcp?%61ccess%5Ftoken=fixture", "/mcp?TOKEN=fixture", "/mcp?bearer=fixture", "/mcp?session=fixture", "/mcp?session_token=fixture", "/mcp?pat=fixture", "/mcp?refresh_token=fixture", "/mcp?id_token=fixture", "/mcp?access_token", "/mcp?authorization=fixture"];
        _results = targets.Select(target =>
        {
            _raw.RawTarget.Returns(target);
            return _gate.Inspect(_request);
        }).ToArray();
    }

    [Fact] void should_refuse_every_supported_credential_query_name() => _results.All(result => result.Refusal == CallerRefusal.CredentialInUri).ShouldBeTrue();
    [Fact] void should_report_a_bad_request() => CallerRefusal.CredentialInUri.HttpStatus().ShouldEqual(400);
}
