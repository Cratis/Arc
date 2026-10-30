// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_caller_is_not_a_member : given.a_tenant_membership_verification
{
    Exception _exception;
    void Establish() => _membership.IsMember(_principal, _tenant).Returns(false);
    void Because() => _exception = Catch.Exception(() => _verification.Verify(_tenant));
    [Fact] void should_deny_membership() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_log_an_information_diagnostic() => _logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Log").GetArguments()[0].ShouldEqual(LogLevel.Information);
    [Fact] void should_not_log_the_candidate() => _logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Log").GetArguments()[2]!.ToString()!.Contains(_tenant.Value, StringComparison.Ordinal).ShouldBeFalse();
}
