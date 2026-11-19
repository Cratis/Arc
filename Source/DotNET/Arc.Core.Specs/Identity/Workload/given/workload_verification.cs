// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload.given;

internal sealed class workload_verification : IDisposable
{
    internal readonly workload_clock Clock = new();
    internal readonly kubernetes_handler Handler = new();
    internal readonly CallerCredentialTarget Target = new("https://resource.example", "workload-audience", "test");
    internal readonly WorkloadCallerOptions Options;
    internal readonly WorkloadVerificationCache Cache;
    internal readonly WorkloadCredentialSource Source;
    internal readonly WorkloadTokenReviewClient Client;
    internal string Reviewer = "reviewer-fixture";

    internal workload_verification(TimeSpan? latency = null, int capacity = 4)
    {
        Options = new()
        {
            DeletedPodRejectionLatency = latency ?? TimeSpan.FromSeconds(30),
            CacheCapacity = capacity,
            Allowlist = new([new("jobs", "worker", "sa-uid", Target.Resource, Target.Environment, new("service-fixture"), new HashSet<string> { "jobs.read" }, new HashSet<string> { "jobs.run" })])
        };
        Cache = new(Options, Clock);
        Client = new(new HttpClient(Handler, disposeHandler: false) { BaseAddress = new("https://kubernetes.example"), Timeout = TimeSpan.FromSeconds(1) }, _ => Task.FromResult(Reviewer));
        Source = CreateSource(Target);
    }

    internal WorkloadCredentialSource CreateSource(CallerCredentialTarget target) => new(Client, Options, target, Cache, Clock);

    internal string Token(int lifetimeSeconds = 300) => TokenAt(Clock.Now.AddSeconds(lifetimeSeconds));

    internal static string TokenAt(DateTimeOffset expiry)
    {
        var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { exp = expiry.ToUnixTimeSeconds() }))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"eyJhbGciOiJSUzI1NiJ9.{body}.fixture-signature";
    }

    internal static CredentialPresentation Present(string token) => new([$"Bearer {token}"], new Dictionary<string, IReadOnlyList<string>>(), shape: PresentedCredentialShape.ServiceAccount);

    internal Task<CredentialVerification> Verify(string? token = null, CancellationToken cancellation = default) => Source.Verify(Present(token ?? Token()), cancellation);

    public void Dispose()
    {
        Client.Dispose();
        Handler.Dispose();
    }
}
