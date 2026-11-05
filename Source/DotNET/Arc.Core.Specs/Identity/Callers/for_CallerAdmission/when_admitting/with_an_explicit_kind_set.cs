// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_CallerAdmission.when_admitting;

public class with_an_explicit_kind_set : Specification
{
    bool _allowed;
    bool _emptyAllowed;
    bool _anonymousAllowed;

    void Because()
    {
        var caller = new VerifiedCallerBuilder(verified_facts.Catalog).Build(verified_facts.Person(), verified_facts.Installation, verified_facts.Tenant).Caller;
        _allowed = CallerAdmission.Allows(caller, new HashSet<CallerPrincipalKind> { CallerPrincipalKind.Person });
        _emptyAllowed = CallerAdmission.Allows(caller, new HashSet<CallerPrincipalKind>());
        _anonymousAllowed = CallerAdmission.Allows(null, new HashSet<CallerPrincipalKind> { CallerPrincipalKind.Person });
    }

    [Fact] void should_allow_the_matching_kind() => _allowed.ShouldBeTrue();
    [Fact] void should_deny_an_empty_kind_set() => _emptyAllowed.ShouldBeFalse();
    [Fact] void should_deny_anonymous() => _anonymousAllowed.ShouldBeFalse();
}
