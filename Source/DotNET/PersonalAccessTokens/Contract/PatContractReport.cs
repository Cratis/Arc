// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>Immutable behavioral results, independent of any test framework.</summary>
public sealed class PatContractReport
{
    /// <summary>Initializes a new instance of the <see cref="PatContractReport"/> class.</summary>
    /// <param name="results">The completed behavioral results.</param>
    internal PatContractReport(IEnumerable<PatContractResult> results) => Results = Array.AsReadOnly(results.ToArray());

    /// <summary>Gets all named results.</summary>
    public IReadOnlyList<PatContractResult> Results { get; }

    /// <summary>Gets whether every required case passed.</summary>
    public bool Passed => Results.Count > 0 && Results.All(result => result.Passed);

    /// <inheritdoc/>
    public override string ToString() => string.Join(Environment.NewLine, Results.Select(result => $"{result.Name}: {(result.Passed ? "passed" : result.Diagnostic)}"));
}
