// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Identity.Workload;

/// <summary>Uses only the configured Kubernetes API and CA, without redirects, proxy inheritance, or alternate trust.</summary>
[IgnoreConvention]
internal sealed class WorkloadTokenReviewClient : IWorkloadTokenReviewClient, IDisposable
{
    readonly HttpClient _http;
    readonly SocketsHttpHandler? _handler;
    readonly Func<CancellationToken, Task<string>> _reviewer;
    readonly X509Certificate2Collection _roots = [];

    /// <summary>Initializes the API client using the explicitly mounted trust material.</summary>
    /// <param name="options">The frozen installation settings.</param>
    /// <exception cref="InvalidOperationException">No configured CA certificate is available.</exception>
    internal WorkloadTokenReviewClient(WorkloadCallerOptions options)
    {
        _roots.ImportFromPemFile(options.CaPath);
        if (_roots.Count == 0) throw new InvalidOperationException("Workload callers require a Kubernetes CA.");
        var policy = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            RevocationMode = X509RevocationMode.NoCheck,
            DisableCertificateDownloads = true
        };
        policy.CustomTrustStore.AddRange(_roots);
        _handler = new()
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            SslOptions = new SslClientAuthenticationOptions { CertificateChainPolicy = policy }
        };
        _http = new(_handler, disposeHandler: false)
        {
            BaseAddress = options.ApiServer,
            Timeout = options.RequestTimeout,
            MaxResponseContentBufferSize = 1_048_576
        };
        _reviewer = cancellation => File.ReadAllTextAsync(options.ReviewerTokenPath, cancellation);
    }

    /// <summary>Initializes a client with an internal test transport and rotating reviewer reader.</summary>
    /// <param name="http">The owned HTTP client.</param>
    /// <param name="reviewer">The reviewer credential reader.</param>
    internal WorkloadTokenReviewClient(HttpClient http, Func<CancellationToken, Task<string>> reviewer)
    {
        _http = http;
        _reviewer = reviewer;
    }

    /// <inheritdoc/>
    public async Task<WorkloadTokenReview?> Review(string token, string audience, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "apis/authentication.k8s.io/v1/tokenreviews")
        {
            Content = JsonContent.Create(new { apiVersion = "authentication.k8s.io/v1", kind = "TokenReview", spec = new { token, audiences = new[] { audience } } })
        };
        using var json = await Send(request, cancellationToken);
        return json is null ? null : WorkloadKubernetesJson.Review(json.RootElement, audience);
    }

    /// <inheritdoc/>
    public async Task<bool> IsLivePod(WorkloadTokenReview review, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/namespaces/{Uri.EscapeDataString(review.Namespace)}/pods/{Uri.EscapeDataString(review.PodName)}");
        using var json = await Send(request, cancellationToken);
        return json is not null && WorkloadKubernetesJson.LivePod(json.RootElement, review);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _http.Dispose();
        _handler?.Dispose();
        foreach (var root in _roots) root.Dispose();
    }

    async Task<JsonDocument?> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var reviewer = (await _reviewer(cancellationToken)).Trim();
        if (string.IsNullOrEmpty(reviewer) || reviewer.Any(char.IsWhiteSpace)) return null;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", reviewer);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(body);
    }
}
