// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

/// <summary>Vector 23.</summary>
public class with_a_pat_in_a_query_value : given.a_resolver
{
    void Establish() => _raw.RawTarget.Returns("/mcp?unrelated=product_pat_fixture");
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_refuse_the_uri_credential() => _result.Refusal.ShouldEqual(CallerRefusal.CredentialInUri);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] async Task should_not_verify_before_presentation_checks() => await _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
