// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Identity.Workload;

public class when_the_workload_builder_is_reused : Specification
{
    readonly given.workload_verification _fixture = new();
    CredentialVerification _result;

    async Task Because()
    {
        var services = new ServiceCollection();
        var builder = new WorkloadCallersBuilder().WithOptions(_fixture.Options);
        builder.Install(services, _fixture.Target);
        builder.WithOptions(_fixture.Options with { Allowlist = new([]) });
        services.Replace(ServiceDescriptor.Singleton<IWorkloadTokenReviewClient>(_fixture.Client));
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_fixture.Clock));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var source = scope.ServiceProvider.GetRequiredService<IVerifiedCredentialSource>();
        _result = await source.Verify(given.workload_verification.Present(_fixture.Token()), CancellationToken.None);
    }

    [Fact] void should_keep_the_installed_options_snapshot() => _result.Facts!.ServicePrincipal!.Value.ShouldEqual("service-fixture");
    void Destroy() => _fixture.Dispose();
}
