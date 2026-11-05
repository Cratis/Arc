// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_distinct_account_issuers : given.a_builder
{
    CallerConstruction _other;

    void Because()
    {
        Build();
        var facts = _facts with { AccountKey = _facts.AccountKey! with { Issuer = "https://other.example.test" } };
        _other = new VerifiedCallerBuilder(_catalog).Build(facts, _installation, _tenant);
    }

    [Fact] void should_not_equate_same_subject_from_different_issuers() => _other.Caller.AccountKey.ShouldNotEqual(_result.Caller.AccountKey);
}
