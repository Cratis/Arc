// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>Test-framework-independent behavioral checks for trusted PAT adapters.</summary>
public static class PatAdapterContract
{
    /// <summary>
    /// Runs every required check against isolated installations. Exceptions are sanitized, never printed.
    /// Cancellation propagates. A passing report is behavioral evidence, not a certification of an adapter.
    /// </summary>
    /// <param name="harness">Adapter-owned persistence and failure controls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Named pass/fail results.</returns>
    public static async Task<PatContractReport> Run(PatAdapterHarness harness, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(harness);
        if (typeof(PatAdapterHarness).GetProperties().Any(property => property.GetValue(harness) is null))
        {
            return new([new("harness", false, "Every harness hook is required.")]);
        }
        var results = new List<PatContractResult>();
        foreach (var (name, check) in new PatContractCases(harness, cancellationToken).Checks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var passed = await check();
                results.Add(new(name, passed, passed ? null : "Required behavior was not observed."));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                results.Add(new(name, false, "Adapter or harness operation failed."));
            }
        }
        return new(results);
    }
}
