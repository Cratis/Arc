// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload;

/// <summary>Authenticated identity and Pod binding returned by Kubernetes, never request claims.</summary>
/// <param name="Namespace">The authenticated Kubernetes namespace.</param>
/// <param name="ServiceAccount">The authenticated service account name.</param>
/// <param name="ServiceAccountUid">The immutable service account UID.</param>
/// <param name="PodName">The bound Pod name.</param>
/// <param name="PodUid">The immutable bound Pod UID.</param>
internal sealed record WorkloadTokenReview(string Namespace, string ServiceAccount, string ServiceAccountUid, string PodName, string PodUid);
