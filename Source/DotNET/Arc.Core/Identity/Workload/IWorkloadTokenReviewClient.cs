// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload;

/// <summary>Internal Kubernetes trust boundary; deliberately not an application-pluggable authentication port.</summary>
internal interface IWorkloadTokenReviewClient
{
    /// <summary>Reviews one bearer for the exact receiving audience.</summary>
    /// <param name="token">The projected bearer token.</param>
    /// <param name="audience">The configured receiving audience.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated Pod-bound identity, or none.</returns>
    Task<WorkloadTokenReview?> Review(string token, string audience, CancellationToken cancellationToken);

    /// <summary>Checks the immutable Pod binding and absence of deletion.</summary>
    /// <param name="review">The authenticated identity and Pod binding.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the Pod still matches and is not deleting.</returns>
    Task<bool> IsLivePod(WorkloadTokenReview review, CancellationToken cancellationToken);
}
