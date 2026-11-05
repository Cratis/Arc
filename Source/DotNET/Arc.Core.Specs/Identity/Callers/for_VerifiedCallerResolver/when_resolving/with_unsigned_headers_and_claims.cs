// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_unsigned_headers_and_claims : given.a_resolver
{
    void Establish()
    {
        _request.User = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "asserted-person")], "host-scheme"));
        _raw.HeaderNames.Returns(["x-ms-client-principal", "x-ms-client-principal-id", "x-ms-client-principal-name"]);
        _raw.GetHeaderValues("Authorization").Returns([]);
        _request.Items["VerifiedCaller"] = "asserted-caller";
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_ignore_unverified_identity() => _result.Refusal.ShouldEqual(CallerRefusal.NoCredential);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_preserve_existing_authentication() => _request.User.Identity.IsAuthenticated.ShouldBeTrue();
    [Fact] async Task should_not_invoke_a_verifier() => await _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
