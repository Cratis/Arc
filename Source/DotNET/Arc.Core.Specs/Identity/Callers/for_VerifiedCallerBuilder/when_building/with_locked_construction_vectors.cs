// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

/// <summary>
/// Projection of locked rows 15–17 and 19–21 at Arc's construction boundary only.
/// Proof outcomes are verifier inputs: these checks do not implement DPoP verification or replay detection.
/// </summary>
public class with_locked_construction_vectors : Specification
{
    readonly Dictionary<int, (JsonElement Expected, CallerConstruction Actual)> _results = [];

    void Because()
    {
        using var stream = typeof(with_locked_construction_vectors).Assembly.GetManifestResourceStream("CallerConstructionVectors.json")!;
        using var document = JsonDocument.Parse(stream);
        document.RootElement.GetProperty("commit").GetString().ShouldEqual("bc7402b9e9a53e8b7114117f0f6022b3690419b8");
        foreach (var vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var row = vector.GetProperty("row").GetInt32();
            var facts = Facts(row, vector.GetProperty("given"));
            var result = new VerifiedCallerBuilder(verified_facts.Catalog).Build(facts, verified_facts.Installation, verified_facts.Tenant);
            _results.Add(row, (vector.GetProperty("then").Clone(), result));
        }
    }

    static VerifiedCredentialFacts Facts(int row, JsonElement input)
    {
        var person = verified_facts.Person();
        return row switch
        {
            15 => person with { ProfileVersion = input.GetProperty("contractVersion").GetString() },
            16 => person with { Client = new(input.GetProperty("patLabel").GetString()!, CallerClientAuthentication.Authenticated) },
            17 => verified_facts.Delegated() with
            {
                Actor = new(input.GetProperty("verifiedTokenActor").GetProperty("iss").GetString()!, input.GetProperty("verifiedTokenActor").GetProperty("sub").GetString()!),
                Grant = verified_facts.Delegated().Grant! with { Actor = new(input.GetProperty("grantActor").GetProperty("iss").GetString()!, input.GetProperty("grantActor").GetProperty("sub").GetString()!) }
            },
            19 => person with { Cnf = input.GetProperty("cnf").GetProperty("jkt").GetString() },
            20 or 21 => person with
            {
                Cnf = input.GetProperty("cnf").GetProperty("jkt").GetString(),
                Proof = new(row == 21 ? CallerProofOutcome.Replayed : CallerProofOutcome.Verified, input.GetProperty("proofKeyThumbprint").GetString(), row == 21 ? input.GetProperty("proofJti").GetString() : "proof-fixture")
            },
            _ => throw new InvalidOperationException($"Unsupported construction vector row {row}")
        };
    }

    [Fact] void should_execute_every_selected_row() => _results.Keys.Order().ToArray().ShouldEqual([15, 16, 17, 19, 20, 21]);
    [Fact] void should_reject_the_unsupported_contract() => (_results[15].Actual.Caller is not null).ShouldEqual(_results[15].Expected.GetProperty("accepted").GetBoolean());
    [Fact] void should_classify_the_unsupported_contract() => _results[15].Actual.Refusal.ShouldEqual(CallerRefusal.UnsupportedProfile);
    [Fact] void should_keep_the_pat_label_declared() => _results[16].Actual.Caller!.Client!.Authentication.ToString().ToLowerInvariant().ShouldEqual(_results[16].Expected.GetProperty("client").GetProperty("authentication").GetString());
    [Fact] void should_preserve_the_declared_label() => _results[16].Actual.Caller!.Client!.Id.ShouldEqual(_results[16].Expected.GetProperty("client").GetProperty("id").GetString());
    [Fact] void should_not_turn_the_label_into_an_actor() => _results[16].Actual.Caller!.Actor.ShouldBeNull();
    [Fact] void should_refuse_the_grant_actor_mismatch() => (_results[17].Actual.Caller is not null).ShouldEqual(_results[17].Expected.GetProperty("callerConstructed").GetBoolean());
    [Fact] void should_classify_the_grant_mismatch() => _results[17].Actual.Refusal.ShouldEqual(CallerRefusal.GrantMismatch);

    [Fact]
    void should_fail_closed_for_missing_mismatched_and_replayed_proof_facts()
    {
        foreach (var row in new[] { 19, 20, 21 })
        {
            (_results[row].Actual.Caller is not null).ShouldEqual(_results[row].Expected.GetProperty("callerConstructed").GetBoolean());
            _results[row].Actual.Refusal!.Value.HttpStatus().ShouldEqual(_results[row].Expected.GetProperty("status").GetInt32());
        }
    }
}
