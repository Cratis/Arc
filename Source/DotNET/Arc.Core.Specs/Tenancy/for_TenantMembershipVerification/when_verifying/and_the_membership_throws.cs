// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_membership_throws : given.a_tenant_membership_verification
{
    Exception _exception;
    void Establish() => _membership.IsMember(_principal, _tenant).Returns(_ => throw new InvalidOperationException("Unavailable"));
    void Because() => _exception = Catch.Exception(() => _verification.Verify(_tenant));
    [Fact] void should_deny_membership() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_log_a_warning() => _logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Log").GetArguments()[0].ShouldEqual(LogLevel.Warning);
    [Fact] void should_include_the_original_failure() => _logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Log").GetArguments()[3].ShouldBeOfExactType<InvalidOperationException>();
}
