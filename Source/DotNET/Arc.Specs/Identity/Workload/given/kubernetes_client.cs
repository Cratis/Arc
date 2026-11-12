// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload.given;

internal sealed class kubernetes_client : IWorkloadTokenReviewClient
{
    internal int Reviews;
    internal bool Accept = true;

    public Task<WorkloadTokenReview?> Review(string token, string audience, CancellationToken cancellationToken)
    {
        Reviews++;
        return Task.FromResult<WorkloadTokenReview?>(Accept && audience == "workload-audience" ? new("jobs", "worker", "sa-uid", "pod-fixture", "pod-uid") : null);
    }

    public Task<bool> IsLivePod(WorkloadTokenReview review, CancellationToken cancellationToken) => Task.FromResult(Accept);
}
