// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

/// <summary>An exact, server-owned Kubernetes identity to capability-ceiling mapping.</summary>
/// <param name="Namespace">The Kubernetes namespace.</param>
/// <param name="ServiceAccount">The service account name.</param>
/// <param name="ServiceAccountUid">The immutable service account UID.</param>
/// <param name="Resource">The receiving resource.</param>
/// <param name="Environment">The receiving environment.</param>
/// <param name="ServicePrincipal">The mapped service principal.</param>
/// <param name="Scopes">The verified scope ceiling.</param>
/// <param name="Permissions">The verified permission ceiling.</param>
public sealed record ServicePrincipalAllowlistEntry(
    string Namespace,
    string ServiceAccount,
    string ServiceAccountUid,
    string Resource,
    string Environment,
    CallerServicePrincipalId ServicePrincipal,
    IReadOnlySet<string> Scopes,
    IReadOnlySet<string> Permissions);
