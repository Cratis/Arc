// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Collects deterministic drift findings.</summary>
internal sealed class DriftReport
{
    readonly HashSet<DriftFinding> _findings = [];

    /// <summary>Adds a finding.</summary>
    /// <param name="kind">Finding category.</param>
    /// <param name="relativePath">Output-relative path.</param>
    public void Add(string kind, string relativePath) => _findings.Add(new(kind, relativePath));

    /// <summary>Gets findings in stable path and category order.</summary>
    /// <returns>Ordered findings.</returns>
    public IEnumerable<DriftFinding> Ordered() => _findings.OrderBy(_ => _.RelativePath, StringComparer.Ordinal).ThenBy(_ => _.Kind, StringComparer.Ordinal);

    /// <summary>Formats findings followed by a summary.</summary>
    /// <returns>The text report.</returns>
    public string ToText() => _findings.Count == 0 ? "Proxies are up to date." :
        string.Join(Environment.NewLine, Ordered().Select(_ => $"{_.Kind}: {_.RelativePath}").Append($"Proxy drift: {_findings.Count} finding(s)."));
}
