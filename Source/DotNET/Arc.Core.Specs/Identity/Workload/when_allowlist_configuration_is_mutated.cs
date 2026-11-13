// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_allowlist_configuration_is_mutated : Specification
{
    readonly given.workload_verification _fixture = new();
    CredentialVerification _result;

    async Task Because()
    {
        var scopes = new HashSet<string> { "jobs.read" };
        var permissions = new HashSet<string> { "jobs.run" };
        var entries = new List<ServicePrincipalAllowlistEntry>
        {
            new("jobs", "worker", "sa-uid", _fixture.Target.Resource, _fixture.Target.Environment, new("service-fixture"), scopes, permissions)
        };
        var options = _fixture.Options with { Allowlist = new(entries) };
        entries.Clear();
        scopes.Add("administrator");
        permissions.Add("all");
        var source = new WorkloadCredentialSource(_fixture.Client, options, _fixture.Target, new(options, _fixture.Clock), _fixture.Clock);
        _result = await source.Verify(given.workload_verification.Present(_fixture.Token()), CancellationToken.None);
    }

    [Fact] void should_keep_the_original_mapping() => _result.Facts!.ServicePrincipal!.Value.ShouldEqual("service-fixture");
    [Fact] void should_keep_the_original_scope_ceiling() => _result.Facts!.Scopes.ShouldContainOnly("jobs.read");
    [Fact] void should_keep_the_original_permission_ceiling() => _result.Facts!.Permissions.ShouldContainOnly("jobs.run");
    void Destroy() => _fixture.Dispose();
}
