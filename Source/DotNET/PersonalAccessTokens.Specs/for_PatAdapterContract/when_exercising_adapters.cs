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
        report.Results.Count.ShouldEqual(22);
    }

    [Theory]
    [InlineData("duplicate verifier")]
    [InlineData("duplicate credential")]
    [InlineData("authoritative lookup")]
    [InlineData("revoke-before-read ordering")]
    [InlineData("owner-bound revocation")]
    [InlineData("authority outage")]
    [InlineData("immutable stored metadata")]
    [InlineData("cancelled create")]
    [InlineData("restore fence")]
    [InlineData("missing recovery evidence")]
    [InlineData("policy outage")]
    [InlineData("positive policy lifetime")]
    [InlineData("concurrent create")]
    [InlineData("monotonic recovery epoch")]
    [InlineData("policy denial")]
    [InlineData("recovery outage")]
    public async Task should_reject_the_named_defect(string defect)
    {
        var report = await PatAdapterContract.Run(Harness(defect), TestContext.Current.CancellationToken);
        report.Passed.ShouldBeFalse();
        report.Results.Single(result => result.Name == defect).Passed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_reject_a_cache_filled_only_by_reads()
    {
        var report = await PatAdapterContract.Run(Harness("cache-filled-on-read"), TestContext.Current.CancellationToken);
        report.Results.Single(result => result.Name == "revoke-before-read ordering").Passed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_sanitize_adapter_exceptions()
    {
        const string marker = "synthetic-private-diagnostic";
        var report = await PatAdapterContract.Run(new() { CreateInstance = () => throw new InvalidOperationException(marker) }, TestContext.Current.CancellationToken);
        report.Passed.ShouldBeFalse();
        report.ToString().Contains(marker, StringComparison.Ordinal).ShouldBeFalse();
        report.Results.All(result => result.Failure?.Contains(marker, StringComparison.Ordinal) != true).ShouldBeTrue();
    }

    [Fact]
    public async Task should_reject_missing_instance_hooks()
    {
        var factory = Harness().CreateInstance!;
        var report = await PatAdapterContract.Run(new() { CreateInstance = () => factory() with { Restore = null } }, TestContext.Current.CancellationToken);
        report.Passed.ShouldBeFalse();
        report.Results.All(result => !result.Passed).ShouldBeTrue();
    }

    [Fact]
    public async Task should_dispose_every_instance()
    {
        var disposed = 0;
        var factory = Harness().CreateInstance!;
        var harness = new PatAdapterHarness
        {
            CreateInstance = () => factory() with
            {
                Dispose = () =>
                {
                    disposed++;
                    return ValueTask.CompletedTask;
                }
            }
        };
        await PatAdapterContract.Run(harness, TestContext.Current.CancellationToken);
        disposed.ShouldEqual(22);
    }
}
