// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_CallerAdmission.when_admitting;

/// <summary>Vector 13.</summary>
public class a_workload_at_a_person_endpoint : Specification
{
    VerifiedCaller _caller;
    bool _allowed;

    void Because()
    {
        var facts = verified_facts.Service() with { PrincipalKind = CallerPrincipalKind.ClusterWorkload, CredentialKind = CallerCredentialKind.Workload };
        _caller = new VerifiedCallerBuilder(verified_facts.Catalog).Build(facts, verified_facts.Installation, null).Caller!;
        _allowed = CallerAdmission.Allows(_caller, new HashSet<CallerPrincipalKind> { CallerPrincipalKind.Person });
    }

    [Fact] void should_have_a_verified_workload() => _caller.ShouldNotBeNull();
    [Fact] void should_deny_even_with_a_permission_ceiling() => _allowed.ShouldBeFalse();
}
