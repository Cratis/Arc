// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>Test-framework-independent behavioral checks for trusted PAT adapters.</summary>
public static class PatAdapterContract
{
    /// <summary>
    /// Runs every required check against isolated installations. Exceptions are sanitized, never printed.
    /// Each case has a one-minute limit. Cancellation propagates. Passing is behavioral evidence, not certification.
    /// </summary>
    /// <param name="harness">Adapter-owned persistence and failure controls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Named pass/fail results.</returns>
    public static async Task<PatContractReport> Run(PatAdapterHarness harness, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(harness);
        if (harness.CreateInstance is null) return new([new("harness", false, "An isolated adapter factory is required.")]);
        var results = new List<PatContractResult>();
        var count = new PatContractCases(cancellationToken).Checks.Count;
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(1));
            var (name, check) = new PatContractCases(timeout.Token).Checks[index];
            try
            {
                var passed = await Execute(harness, check, timeout.Token).WaitAsync(timeout.Token);
                results.Add(new(name, passed, passed ? null : "Required behavior was not observed."));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                results.Add(new(name, false, "Adapter or harness operation failed or timed out."));
            }
        }
        return new(results);
    }

    static async Task<bool> Execute(PatAdapterHarness harness, Func<PatContractFixture, Task<bool>> check, CancellationToken cancellationToken)
    {
        await using var adapter = harness.CreateInstance!();
        if (!adapter.HasRequiredHooks()) return false;
        var epoch = await adapter.RecoveryEpoch.Read(cancellationToken) ?? throw new InvalidOperationException();
        return await check(new(adapter, epoch));
    }
}
