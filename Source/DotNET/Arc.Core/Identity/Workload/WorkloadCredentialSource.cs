// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Identity.Callers;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Identity.Workload;

/// <summary>Verifies pod-bound projected tokens, never JWT claims or unsigned workload headers alone.</summary>
/// <param name="client">The internal Kubernetes trust boundary.</param>
/// <param name="options">The frozen installation configuration.</param>
/// <param name="target">The receiving target.</param>
/// <param name="cache">The bounded positive cache.</param>
/// <param name="clock">The verification clock.</param>
[IgnoreConvention]
internal sealed class WorkloadCredentialSource(
    IWorkloadTokenReviewClient client,
    WorkloadCallerOptions options,
    CallerCredentialTarget target,
    WorkloadVerificationCache cache,
    TimeProvider clock) : IVerifiedCredentialSource
{
    /// <inheritdoc/>
    public PresentedCredentialShape Shape => PresentedCredentialShape.ServiceAccount;

    /// <inheritdoc/>
    public bool Consumes(string headerName) => headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<CredentialVerification> Verify(CredentialPresentation presentation, CancellationToken cancellationToken)
    {
        if (presentation.Shape != Shape) return CredentialVerification.NotApplicable();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (presentation.Authorization.Count != 1 || presentation.Envelope.Count != 0) return Refuse();
            var authorization = presentation.Authorization[0];
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Refuse();
            var token = authorization[7..];
            if (token.Length > 65_536) return Refuse();
            var cached = cache.Find(token, target);
            if (cached is not null) return CredentialVerification.Verified(cached);
            return await Review(token, cancellationToken);
        }
        catch (Exception)
        {
            // Outages, malformed responses, cancellation and unreadable rotating credentials all refuse.
            return Refuse();
        }
    }

    static CredentialVerification Refuse() => CredentialVerification.Refused(CallerRefusal.SourceRefused);

    async Task<CredentialVerification> Review(string token, CancellationToken cancellationToken)
    {
        var started = clock.GetUtcNow();
        var expiry = WorkloadKubernetesJson.Expiry(token);
        if (expiry <= started) return Refuse();
        var review = await client.Review(token, target.Audience, cancellationToken);
        if (review is null) return Refuse();
        var mapping = options.Allowlist.Find(review, target);
        if (mapping is null || !await client.IsLivePod(review, cancellationToken)) return Refuse();
        cancellationToken.ThrowIfCancellationRequested();
        if (expiry <= clock.GetUtcNow()) return Refuse();
        var facts = new VerifiedCredentialFacts(
            CallerPrincipalKind.ClusterWorkload,
            CallerCredentialKind.Workload,
            new CredentialId(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))),
            target)
        {
            ServicePrincipal = mapping.ServicePrincipal,
            Scopes = mapping.Scopes,
            Permissions = mapping.Permissions
        };
        cache.Store(token, target, facts, review.PodUid, expiry, started);
        return CredentialVerification.Verified(facts);
    }
}
