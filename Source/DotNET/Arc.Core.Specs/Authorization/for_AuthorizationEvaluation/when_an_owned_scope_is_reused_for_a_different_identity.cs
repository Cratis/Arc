// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_an_owned_scope_is_reused_for_a_different_identity : Specification
{
    Exception? _afterPreviousWork;
    Exception? _afterAnotherPrincipal;
    Exception? _sameIdentity;

    void Because()
    {
        var services = Substitute.For<IServiceProvider>();
        using (AuthorizationExecutionScopes.Begin(services))
        {
            AuthorizationExecutionScopes.MarkWorkStarted(services);
            _afterPreviousWork = Catch.Exception(() => AuthorizationExecutionScopes.Bind(services, Principal("tenant-B")));
        }

        using (AuthorizationExecutionScopes.Begin(services))
        {
            AuthorizationExecutionScopes.Bind(services, Principal("tenant-B"));
            AuthorizationExecutionScopes.MarkWorkStarted(services);
            _sameIdentity = Catch.Exception(() => AuthorizationExecutionScopes.Bind(services, Principal("tenant-B")));
            _afterAnotherPrincipal = Catch.Exception(() => AuthorizationExecutionScopes.Bind(services, Principal("tenant-C")));
        }
    }

    [Fact] void should_reject_switching_identity_after_services_were_resolved() => _afterPreviousWork.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_allow_the_same_complete_identity_on_the_owned_scope() => _sameIdentity.ShouldBeNull();
    [Fact] void should_reject_another_identity_in_a_previous_selection_scope() => _afterAnotherPrincipal.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();

    static ClaimsPrincipal Principal(string tenant) => new(new ClaimsIdentity([new Claim("tenant_id", tenant)], "Special"));
}
