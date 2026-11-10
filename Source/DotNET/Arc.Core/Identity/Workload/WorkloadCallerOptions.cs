// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload;

/// <summary>Explicit Kubernetes trust and bounded workload verification settings.</summary>
public sealed record WorkloadCallerOptions
{
    /// <summary>Gets the HTTPS Kubernetes API server. Redirects and alternate authorities are never followed.</summary>
    public Uri ApiServer { get; init; } = new("https://kubernetes.default.svc");

    /// <summary>Gets the mounted reviewer token path, reread for each API call to support rotation.</summary>
    public string ReviewerTokenPath { get; init; } = "/var/run/secrets/kubernetes.io/serviceaccount/token";

    /// <summary>Gets the PEM CA path used exclusively to authenticate the API server.</summary>
    public string CaPath { get; init; } = "/var/run/secrets/kubernetes.io/serviceaccount/ca.crt";

    /// <summary>Gets the maximum positive-cache lifetime and deleted-Pod rejection latency.</summary>
    public TimeSpan DeletedPodRejectionLatency { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets the hard bound on positive cache entries.</summary>
    public int CacheCapacity { get; init; } = 1024;

    /// <summary>Gets the timeout for each Kubernetes API call.</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets the immutable service identity allowlist.</summary>
    public ServicePrincipalAllowlist Allowlist { get; init; } = new([]);

    /// <summary>Gets whether trust, mappings and resource bounds are explicitly valid.</summary>
    internal bool IsValid => ValidServer && ValidBounds && Allowlist.IsValid &&
        !string.IsNullOrWhiteSpace(ReviewerTokenPath) && !string.IsNullOrWhiteSpace(CaPath);

    bool ValidServer => ApiServer.IsAbsoluteUri && ApiServer.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(ApiServer.UserInfo) && string.IsNullOrEmpty(ApiServer.Query) &&
        string.IsNullOrEmpty(ApiServer.Fragment) && ApiServer.AbsolutePath == "/";

    bool ValidBounds => CacheCapacity is > 0 and <= 100_000 &&
        DeletedPodRejectionLatency > TimeSpan.Zero && DeletedPodRejectionLatency <= TimeSpan.FromMinutes(5) &&
        RequestTimeout > TimeSpan.Zero && RequestTimeout <= TimeSpan.FromSeconds(30);
}
