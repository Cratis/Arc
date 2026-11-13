// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace OAuthPatCallerAcceptance.Specs.for_RefusalBody;

public class when_native_refusals_are_checked : Specification
{
    bool _tokenLeak;
    bool _hashLeak;
    bool _claimLeak;
    bool _verifierLeak;
    bool _wrongMeaning;
    bool _nativeShapes;

    void Because()
    {
        _tokenLeak = Mcp("""{"error":"unauthorized","token":"synthetic-token"}""");
        _hashLeak = Rest("""{"type":"about:blank","title":"synthetic-hash","status":401}""");
        _claimLeak = Rest("""{"type":"about:blank","title":"Unauthorized","status":401,"subject":"synthetic-person"}""");
        _verifierLeak = Mcp("""{"error":"synthetic-verifier-failure"}""");
        _wrongMeaning = Mcp("""{"error":"forbidden"}""");
        _nativeShapes = Mcp("""{"error":"unauthorized"}""") && Rest("""{"type":"about:blank","title":"Unauthorized","status":401}""");
    }

    [Fact] void should_reject_token_details() => _tokenLeak.ShouldBeFalse();
    [Fact] void should_reject_hash_details() => _hashLeak.ShouldBeFalse();
    [Fact] void should_reject_claim_details() => _claimLeak.ShouldBeFalse();
    [Fact] void should_reject_verifier_details() => _verifierLeak.ShouldBeFalse();
    [Fact] void should_reject_a_different_error_meaning() => _wrongMeaning.ShouldBeFalse();
    [Fact] void should_accept_both_native_shapes_with_the_same_meaning() => _nativeShapes.ShouldBeTrue();

    static bool Mcp(string body) => given.RefusalBody.IsSafe("/mcp", HttpStatusCode.Unauthorized, "application/json", body);
    static bool Rest(string body) => given.RefusalBody.IsSafe("/rest", HttpStatusCode.Unauthorized, "application/problem+json", body);
}
