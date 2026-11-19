// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Workload;

public class when_installation_is_incomplete : Specification
{
    readonly List<Exception?> _errors = [];
    readonly List<int> _registrations = [];

    void Because()
    {
        var valid = new WorkloadCallerOptions
        {
            Allowlist = new([new("jobs", "worker", "sa-uid", "https://resource.example", "test", new("worker"), new HashSet<string>(), new HashSet<string>())])
        };
        var invalid = new[]
        {
            valid with { ApiServer = new("http://kubernetes.example") },
            valid with { ApiServer = new("https://kubernetes.example/redirect") },
            valid with { DeletedPodRejectionLatency = TimeSpan.Zero },
            valid with { DeletedPodRejectionLatency = TimeSpan.FromMinutes(6) },
            valid with { CacheCapacity = 0 },
            valid with { RequestTimeout = TimeSpan.FromMinutes(1) },
            valid with { Allowlist = new([]) },
            valid with { ReviewerTokenPath = "/missing/reviewer-fixture", CaPath = "/missing/ca-fixture" }
        };
        foreach (var options in invalid)
        {
            var services = new ServiceCollection();
            var builder = new VerifiedCallersBuilder().ForResource("https://resource.example", "workload-audience", "test")
                .UseWorkloadCallers(workload => workload.WithOptions(options));
            _errors.Add(Record.Exception(() => services.AddVerifiedCallers(builder)));
            _registrations.Add(services.Count);
        }
    }

    [Fact] void should_reject_every_incomplete_installation() => _errors.TrueForAll(error => error is InvalidVerifiedCallerInstallation).ShouldBeTrue();
    [Fact] void should_not_partially_register_the_installation() => _registrations.TrueForAll(count => count == 0).ShouldBeTrue();
}
