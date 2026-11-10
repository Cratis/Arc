// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_verifying_a_pod_bound_token : Specification
{
    readonly given.workload_verification _fixture = new();
    CredentialVerification _result;

    async Task Because() => _result = await _fixture.Verify();

    [Fact] void should_check_the_exact_receiving_audience() => _fixture.Handler.SentAudience.ShouldEqual(_fixture.Target.Audience);
    [Fact] void should_read_the_bound_pod() => _fixture.Handler.Pods.ShouldEqual(1);
    [Fact] void should_return_only_workload_authority() => _result.Facts!.PrincipalKind.ShouldEqual(CallerPrincipalKind.ClusterWorkload);
    [Fact] void should_identify_the_service_account_credential() => _result.Facts!.CredentialKind.ShouldEqual(CallerCredentialKind.Workload);
    [Fact] void should_map_the_service() => _result.Facts!.ServicePrincipal!.Value.ShouldEqual("service-fixture");
    [Fact] void should_apply_the_scope_ceiling() => _result.Facts!.Scopes.ShouldContainOnly("jobs.read");
    [Fact] void should_apply_the_permission_ceiling() => _result.Facts!.Permissions.ShouldContainOnly("jobs.run");
    [Fact] void should_not_supply_a_tenant() => _result.Facts!.Tenant.ShouldBeNull();
    [Fact] void should_not_supply_a_person() => _result.Facts!.Person.ShouldBeNull();
    [Fact] void should_not_supply_an_account() => _result.Facts!.AccountKey.ShouldBeNull();
    [Fact] void should_not_supply_an_organization() => _result.Facts!.Organisation.ShouldBeNull();
    [Fact] void should_not_supply_a_grant() => _result.Facts!.Grant.ShouldBeNull();
    [Fact] void should_not_expose_the_token_as_an_identifier() => _result.Facts!.CredentialId.Value.ShouldNotEqual(_fixture.Token());

    void Destroy() => _fixture.Dispose();
}
