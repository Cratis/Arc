// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

/// <summary>A frozen, exact-match service principal allowlist. Missing and ambiguous entries are refused.</summary>
public sealed class ServicePrincipalAllowlist
{
    readonly ServicePrincipalAllowlistEntry[] _entries;

    /// <summary>Initializes a snapshot of the allowlist and its capability ceilings.</summary>
    /// <param name="entries">The server-owned identity mappings.</param>
    public ServicePrincipalAllowlist(IEnumerable<ServicePrincipalAllowlistEntry> entries)
    {
        _entries = entries.Select(entry => entry with
        {
            Scopes = entry.Scopes.ToFrozenSet(StringComparer.Ordinal),
            Permissions = entry.Permissions.ToFrozenSet(StringComparer.Ordinal)
        }).ToArray();
    }

    /// <summary>Gets whether all mappings are nonempty and unambiguous.</summary>
    internal bool IsValid => _entries.Length > 0 && _entries.All(IsValidEntry) &&
        _entries.GroupBy(entry => (entry.Namespace, entry.ServiceAccount, entry.ServiceAccountUid, entry.Resource, entry.Environment)).All(group => group.Count() == 1);

    /// <summary>Finds the exact authenticated service account and receiving target.</summary>
    /// <param name="review">The authenticated Kubernetes identity.</param>
    /// <param name="target">The receiving target.</param>
    /// <returns>The unique mapping, or none.</returns>
    internal ServicePrincipalAllowlistEntry? Find(WorkloadTokenReview review, CallerCredentialTarget target) =>
        _entries.SingleOrDefault(entry => entry.Namespace == review.Namespace && entry.ServiceAccount == review.ServiceAccount &&
            entry.ServiceAccountUid == review.ServiceAccountUid && entry.Resource == target.Resource && entry.Environment == target.Environment);

    static bool IsValidEntry(ServicePrincipalAllowlistEntry entry) =>
        new[] { entry.Namespace, entry.ServiceAccount, entry.ServiceAccountUid, entry.Resource, entry.Environment, entry.ServicePrincipal.Value }
            .All(value => !string.IsNullOrWhiteSpace(value)) && entry.Scopes.Concat(entry.Permissions).All(value => !string.IsNullOrWhiteSpace(value));
}
