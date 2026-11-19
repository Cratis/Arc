// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload;

/// <summary>The Kubernetes TokenReview request body.</summary>
/// <param name="ApiVersion">The Kubernetes API version.</param>
/// <param name="Kind">The Kubernetes resource kind.</param>
/// <param name="Spec">The token and audiences to review.</param>
internal sealed record TokenReviewRequest(string ApiVersion, string Kind, TokenReviewSpec Spec)
{
    /// <summary>Creates a TokenReview request for one token bound to one audience.</summary>
    /// <param name="token">The bearer token to review.</param>
    /// <param name="audience">The receiving audience.</param>
    /// <returns>The request body.</returns>
    internal static TokenReviewRequest For(string token, string audience) =>
        new("authentication.k8s.io/v1", "TokenReview", new(token, [audience]));
}
