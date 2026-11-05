// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

public class when_verifying_shared_vectors : Specification
{
    readonly List<string> _mismatches = [];
    readonly List<string> _skipped = [];
    int _matched;
    int _canonicalMatches;

    async Task Because()
    {
        var groups = new Dictionary<string, IForwardedIdentityNonceStore>(StringComparer.Ordinal);
        foreach (var vector in ForwardedIdentityV2Vectors.Fixture.Cases)
        {
            if (vector.Name == "unauthenticated-hop")
            {
                _skipped.Add(vector.Name);
                continue;
            }
            var store = new ProcessLocalForwardedIdentityNonceStore();
            IForwardedIdentityNonceStore replayStore = store;
            if (vector.ReplayGroup is { } group && !groups.TryGetValue(group, out replayStore!)) groups.Add(group, replayStore = store);
            var request = VerificationFixture.Request(vector);
            var settings = VerificationFixture.Settings(vector.Inputs);
            var verifier = VerificationFixture.Verifier(vector, new VectorNonceStore(replayStore, vector.Inputs.StoreAvailable, vector.Inputs.StoreFull));
            var result = await verifier.Verify(settings, request, CancellationToken.None);
            var callerSource = result.CallerSource switch
            {
                ForwardedIdentityV2CallerSource.Pat => "pat-verifier",
                ForwardedIdentityV2CallerSource.OAuth => "oauth-verifier",
                _ => null
            };
            var actual = new Outcome(result.Rejection is null ? "accept" : "reject", result.Rejection?.ToString().ToLowerInvariant() ?? "ok", callerSource);
            if (actual != vector.Expected)
            {
                _mismatches.Add($"{vector.Name}: expected {vector.Expected.Result}/{vector.Expected.Reason}/{vector.Expected.CallerSource}; got {actual.Result}/{actual.Reason}/{actual.CallerSource}");
            }
            else
            {
                _matched++;
            }
            if (vector.Expected.Result == "accept")
            {
                var bytes = ForwardedIdentityV2Canonicalization.Create(settings, request, vector.Envelope.Split('.')).Bytes;
                if (!bytes.SequenceEqual(Encoding.UTF8.GetBytes(vector.SigningInput))) _mismatches.Add(vector.Name + ": canonical bytes differ");
                else _canonicalMatches++;
            }
        }
    }

    [Fact] void should_match_every_non_hop_outcome() => _mismatches.ShouldBeEmpty();
    [Fact] void should_exercise_the_whole_non_hop_fixture() => _matched.ShouldEqual(219);
    [Fact] void should_check_canonical_bytes_for_every_accepted_case() => _canonicalMatches.ShouldEqual(ForwardedIdentityV2Vectors.Fixture.Cases.Count(vector => vector.Expected.Result == "accept"));
    [Fact] void should_defer_only_hop_admission() => _skipped.ShouldEqual(new[] { "unauthenticated-hop" });
}
