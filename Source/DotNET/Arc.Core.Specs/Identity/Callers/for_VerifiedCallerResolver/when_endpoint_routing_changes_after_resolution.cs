// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver;

public class when_endpoint_routing_changes_after_resolution : given.a_resolver
{
    CallerResolution _before;

    async Task Because()
    {
        _before = await _resolver.GetCurrent();
        _request.Items[ExpectedCredentialShape.Key] = new ExpectedCredentialShape(PresentedCredentialShape.ServiceAccount);
        _result = await _resolver.GetCurrent();
    }

    [Fact] void should_first_resolve_the_original_shape() => _before.Caller.ShouldNotBeNull();
    [Fact] void should_not_reuse_authority_under_a_new_shape() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_not_probe_another_source() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
