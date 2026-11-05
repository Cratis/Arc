// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

/// <summary>Vectors 19, 20, 21.</summary>
public class with_sender_binding : given.a_builder
{
    CallerConstruction[] _results;

    void Because()
    {
        var proofs = new[]
        {
            new CallerProof(CallerProofOutcome.NotPresented),
            new CallerProof(CallerProofOutcome.WrongKey),
            new CallerProof(CallerProofOutcome.Verified, "another-key", "proof-fixture"),
            new CallerProof(CallerProofOutcome.Replayed, "key-fixture", "proof-fixture"),
            new CallerProof(CallerProofOutcome.Verified, "key-fixture", "proof-fixture")
        };
        _results = proofs.Select(proof => new VerifiedCallerBuilder(_catalog).Build(_facts with { Cnf = "key-fixture", Proof = proof }, _installation, _tenant)).ToArray();
    }

    [Fact] void should_require_a_proof() => _results[0].Refusal.ShouldEqual(CallerRefusal.SenderBindingRequired);
    [Fact] void should_refuse_the_wrong_key() => _results[1].Refusal.ShouldEqual(CallerRefusal.SenderBindingMismatch);
    [Fact] void should_refuse_a_verified_different_key() => _results[2].Refusal.ShouldEqual(CallerRefusal.SenderBindingMismatch);
    [Fact] void should_refuse_replay() => _results[3].Refusal.ShouldEqual(CallerRefusal.SenderBindingReplayed);
    [Fact] void should_not_construct_a_caller_on_failure() => _results.Take(4).All(result => result.Caller is null).ShouldBeTrue();
    [Fact] void should_report_unauthorized_for_failed_proofs() => _results.Take(4).All(result => result.Refusal!.Value.HttpStatus() == 401).ShouldBeTrue();
    [Fact] void should_retain_only_the_verified_binding() => _results[4].Caller.SenderBinding.ShouldEqual(new CallerSenderBinding("key-fixture", "proof-fixture"));
}
