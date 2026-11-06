// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_different_installation : given.a_resolver
{
    async Task Because()
    {
        await _resolver.GetCurrent();
        var installation = new VerifiedCallerInstallation(verified_facts.Installation.Resource, verified_facts.Installation.Environment);
        var resolver = new VerifiedCallerResolver(_requests, _sources, new(), new(verified_facts.Catalog), installation, _tenants);
        _result = await resolver.GetCurrent();
    }
    [Fact] void should_refuse() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
