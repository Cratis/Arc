// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_missing_person_fields : given.a_builder
{
    CallerConstruction[] _results;

    void Because()
    {
        VerifiedCredentialFacts[] invalid =
        [
            _facts with { CredentialId = new("") },
            _facts with { Person = null },
            _facts with { Assurance = null },
            _facts with { Assurance = _facts.Assurance! with { Idp = "" } },
            _facts with { Assurance = _facts.Assurance! with { AuthTime = 0 } },
            _facts with { Assurance = _facts.Assurance! with { Acr = " " } },
            _facts with { PrincipalKind = (CallerPrincipalKind)99 },
            _facts with { CredentialKind = (CallerCredentialKind)99 }
        ];
        _results = invalid.Select(facts => new VerifiedCallerBuilder(_catalog).Build(facts, _installation, _tenant)).ToArray();
    }

    [Fact] void should_require_complete_verified_person_facts() => _results.All(result => result.Refusal == CallerRefusal.MissingRequiredField).ShouldBeTrue();
    [Fact] void should_never_construct_a_caller() => _results.All(result => result.Caller is null).ShouldBeTrue();
}
