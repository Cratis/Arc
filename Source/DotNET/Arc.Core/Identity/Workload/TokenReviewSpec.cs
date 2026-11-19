// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload;

/// <summary>The spec part of a Kubernetes TokenReview request.</summary>
/// <param name="Token">The bearer token to review.</param>
/// <param name="Audiences">The audiences the token must be bound to.</param>
internal sealed record TokenReviewSpec(string Token, string[] Audiences);
