// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_CallerAdmission.when_admitting;

public class a_service_at_a_person_endpoint : Specification
{
    bool _allowed;

    void Because()
    {
        var caller = new VerifiedCallerBuilder(verified_facts.Catalog).Build(verified_facts.Service(), verified_facts.Installation, null).Caller;
        _allowed = CallerAdmission.Allows(caller, new HashSet<CallerPrincipalKind> { CallerPrincipalKind.Person });
    }

    [Fact] void should_deny_despite_permissions() => _allowed.ShouldBeFalse();
}
