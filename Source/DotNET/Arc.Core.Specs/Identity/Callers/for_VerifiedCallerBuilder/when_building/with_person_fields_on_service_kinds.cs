// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_person_fields_on_service_kinds : given.a_builder
{
    CallerConstruction[] _results;

    void Because()
    {
        var service = verified_facts.Service();
        var person = verified_facts.Person();
        VerifiedCredentialFacts[] invalid =
        [
            service with { Person = person.Person },
            service with { AccountKey = person.AccountKey },
            service with { Assurance = person.Assurance },
            service with { Actor = verified_facts.Actor },
            service with { Grant = verified_facts.Delegated().Grant }
        ];
        CallerPrincipalKind[] kinds = [CallerPrincipalKind.OrganizationAutomation, CallerPrincipalKind.ClusterWorkload, CallerPrincipalKind.Runner];
        _results = kinds.SelectMany(kind => invalid.Select(facts => new VerifiedCallerBuilder(_catalog).Build(facts with { PrincipalKind = kind }, _installation, TenantId.NotSet))).ToArray();
    }

    [Fact] void should_refuse_person_and_delegation_fields_for_all_service_kinds() => _results.All(result => result.Refusal == CallerRefusal.ServiceCarriesPersonFields).ShouldBeTrue();
    [Fact] void should_never_construct_a_caller() => _results.All(result => result.Caller is null).ShouldBeTrue();
}
