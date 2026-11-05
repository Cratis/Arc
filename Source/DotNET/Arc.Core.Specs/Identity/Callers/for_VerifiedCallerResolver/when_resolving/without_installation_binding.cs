// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class without_installation_binding : given.a_resolver
{
    void Establish() => _resolver = new(_requests, _sources, new(), new(verified_facts.Catalog), new(null, null), _tenants);
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_refuse_unconfigured_receiving_identity() => _result.Refusal.ShouldEqual(CallerRefusal.InstallationNotConfigured);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
