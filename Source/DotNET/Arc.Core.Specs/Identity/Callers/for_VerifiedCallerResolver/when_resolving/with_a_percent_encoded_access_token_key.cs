// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

/// <summary>Vector 23.</summary>
public class with_a_percent_encoded_access_token_key : given.a_resolver
{
    void Establish() => _raw.RawTarget.Returns("/mcp?%61ccess%5Ftoken=fixture");
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_refuse_before_verification() => _result.Refusal.ShouldEqual(CallerRefusal.CredentialInUri);
    [Fact] async Task should_not_call_a_source() => await _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
