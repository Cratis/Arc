// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_the_envelope_changed_after_resolution : given.a_resolver
{
    async Task Because()
    {
        await _resolver.GetCurrent();
        _raw.HeaderNames.Returns(["Authorization", ForwardedIdentityV2Envelope.Header]);
        _raw.GetHeaderValues(ForwardedIdentityV2Envelope.Header).Returns(["v2.synthetic-fixture"]);
        _result = await _resolver.GetCurrent();
    }
    [Fact] void should_refuse() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
