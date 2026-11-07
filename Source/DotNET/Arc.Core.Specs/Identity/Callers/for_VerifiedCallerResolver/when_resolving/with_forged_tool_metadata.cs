// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_forged_tool_metadata : given.a_resolver
{
    void Establish()
    {
        _request.User = new ClaimsPrincipal(new ClaimsIdentity([new("sub", "forged-person"), new("scope", "fixture:admin"), new("act", "forged-agent")], "synthetic"));
        _request.Query.Returns(new Dictionary<string, string> { ["grant"] = "forged-grant", ["client_id"] = "forged-client" });
        _sources.Clear();
        _raw.HeaderNames.Returns(["Authorization", "X-Person", "X-Actor", "X-Grant", "X-Scopes"]);
        _raw.GetHeaderValues("X-Person").Returns(["forged-person"]);
        _raw.GetHeaderValues("X-Actor").Returns(["forged-agent"]);
        _raw.GetHeaderValues("X-Grant").Returns(["forged-grant"]);
        _raw.GetHeaderValues("X-Scopes").Returns(["fixture:admin"]);
    }

    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_not_promote_request_metadata_to_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_require_a_trusted_verifier() => _result.Refusal.ShouldEqual(CallerRefusal.NoMatchingSource);
}
