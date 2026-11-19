// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Workload;

public class when_configuring_the_deleted_pod_rejection_latency : Specification
{
    [Theory]
    [InlineData(30, true)]
    [InlineData(31, false)]
    [InlineData(300, false)]
    public void should_enforce_the_thirty_second_installation_limit(int seconds, bool accepted)
    {
        var services = new ServiceCollection();
        var options = new WorkloadCallerOptions
        {
            DeletedPodRejectionLatency = TimeSpan.FromSeconds(seconds),
            Allowlist = new([new("jobs", "worker", "sa-uid", "https://resource.example", "test", new("worker"), new HashSet<string>(), new HashSet<string>())])
        };
        options.IsValid.ShouldEqual(accepted);
        if (accepted) return;

        var builder = new VerifiedCallersBuilder().ForResource("https://resource.example", "workload-audience", "test")
            .UseWorkloadCallers(workload => workload.WithOptions(options));
        var error = Record.Exception(() => services.AddVerifiedCallers(builder));
        error.ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
        error!.Message.ShouldContain("bounded positive-cache settings");
        services.ShouldBeEmpty();
    }
}
