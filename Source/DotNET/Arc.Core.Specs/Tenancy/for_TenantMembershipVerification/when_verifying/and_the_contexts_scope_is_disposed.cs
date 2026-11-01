// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_contexts_scope_is_disposed : given.a_membership_verification_with_real_scopes
{
    bool _first;
    Exception _exception;

    void Because()
    {
        BuildRoot(ServiceLifetime.Scoped);
        var scope = _root.CreateScope();
        _requests.Current = Subscription(scope.ServiceProvider, "alice");
        _first = _verification.Verify(new TenantId("t"));
        scope.Dispose();
        _exception = Catch.Exception(() => _verification.Verify(new TenantId("t")));
    }

    [Fact] void should_allow_before_disposal() => _first.ShouldBeTrue();
    [Fact] void should_deny_after_disposal() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_not_run_the_check_through_the_disposed_scope() => _created.Single().Calls.Count.ShouldEqual(1);
    [Fact] void should_log_a_resolution_failure() => _logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log" && (LogLevel)_.GetArguments()[0]! == LogLevel.Warning).ShouldBeTrue();
}
