// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_CallerAdmission.when_admitting;

public class a_person_at_a_service_endpoint : Specification
{
    bool _allowed;

    void Because()
    {
        var caller = new VerifiedCallerBuilder(verified_facts.Catalog).Build(verified_facts.Person(), verified_facts.Installation, verified_facts.Tenant).Caller;
        _allowed = CallerAdmission.Allows(caller, new HashSet<CallerPrincipalKind> { CallerPrincipalKind.OrganizationAutomation });
    }

    [Fact] void should_require_explicit_person_admission() => _allowed.ShouldBeFalse();
}
