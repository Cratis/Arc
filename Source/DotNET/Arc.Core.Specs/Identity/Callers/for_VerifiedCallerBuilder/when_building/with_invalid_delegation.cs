// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

/// <summary>Vectors 17, 18.</summary>
public class with_invalid_delegation : given.a_builder
{
    CallerConstruction[] _results;

    void Because()
    {
        var facts = verified_facts.Delegated();
        var grant = facts.Grant!;
        VerifiedCredentialFacts[] hostile =
        [
            facts with { Grant = null },
            facts with { Grant = grant with { Actor = new("https://agents.example.test", "agent-B") } },
            facts with { Grant = grant with { Audience = "other-api" } },
            facts with { Grant = grant with { Resource = "https://other.example.test" } },
            facts with { Grant = grant with { Environment = "production" } },
            facts with { Grant = grant with { Tenant = new("other-tenant") } },
            facts with { Grant = grant with { AccountKey = grant.AccountKey with { Issuer = "https://other.example.test" } } }
        ];
        _results = hostile.Select(value => new VerifiedCallerBuilder(_catalog).Build(value, _installation, _tenant)).ToArray();
    }

    [Fact] void should_require_a_verified_grant() => _results[0].Refusal.ShouldEqual(CallerRefusal.GrantMissing);
    [Fact] void should_require_every_grant_binding_to_match() => _results.Skip(1).All(result => result.Refusal == CallerRefusal.GrantMismatch).ShouldBeTrue();
    [Fact] void should_never_construct_a_caller() => _results.All(result => result.Caller is null).ShouldBeTrue();
}
