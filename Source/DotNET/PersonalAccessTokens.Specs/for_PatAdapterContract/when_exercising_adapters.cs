// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens.Contract;

namespace Cratis.Arc.PersonalAccessTokens.for_PatAdapterContract;

public class when_exercising_adapters : given.a_contract_harness
{
    [Fact]
    public async Task should_accept_a_correct_adapter()
    {
        var report = await PatAdapterContract.Run(Harness(), TestContext.Current.CancellationToken);
        report.Results.Where(result => !result.Passed).ShouldBeEmpty();
        report.Results.Count.ShouldEqual(24);
    }

    [Theory]
    [InlineData("atomic issuance")]
    [InlineData("failed issuance rollback")]
    [InlineData("resource partition")]
    [InlineData("duplicate credential")]
    [InlineData("duplicate verifier")]
    [InlineData("wrong resource owner")]
    [InlineData("revocation idempotency")]
    [InlineData("second-process revocation")]
    [InlineData("restore fence")]
    [InlineData("missing recovery evidence")]
    [InlineData("unavailable authority")]
    [InlineData("unavailable recovery")]
    [InlineData("unavailable policy")]
    [InlineData("disabled person")]
    public async Task should_reject_the_named_defect(string defect)
    {
        var report = await PatAdapterContract.Run(Harness(defect), TestContext.Current.CancellationToken);
        report.Passed.ShouldBeFalse();
        report.Results.Single(result => result.Name == defect).Passed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_sanitize_adapter_exceptions()
    {
        const string marker = "synthetic-private-diagnostic";
        var harness = Harness();
        var hostile = new PatAdapterHarness
        {
            Create = _ => throw new InvalidOperationException(marker),
            HasIssuedIntent = harness.HasIssuedIntent,
            FailNextCommit = harness.FailNextCommit,
            SecondProcess = harness.SecondProcess,
            Snapshot = harness.Snapshot,
            Restore = harness.Restore,
            AdvanceRecoveryEpoch = harness.AdvanceRecoveryEpoch,
            RemoveRecoveryEvidence = harness.RemoveRecoveryEvidence,
            BlockAuthority = harness.BlockAuthority,
            BlockRecovery = harness.BlockRecovery,
            BlockPolicy = harness.BlockPolicy,
            DisablePerson = harness.DisablePerson,
            RemoveMembership = harness.RemoveMembership,
            ReduceLifetime = harness.ReduceLifetime,
            ReduceScopes = harness.ReduceScopes,
            ExpireSso = harness.ExpireSso
        };
        var report = await PatAdapterContract.Run(hostile, TestContext.Current.CancellationToken);
        report.Passed.ShouldBeFalse();
        report.ToString().Contains(marker, StringComparison.Ordinal).ShouldBeFalse();
        report.Results.All(result => result.Diagnostic?.Contains(marker, StringComparison.Ordinal) != true).ShouldBeTrue();
    }
}
