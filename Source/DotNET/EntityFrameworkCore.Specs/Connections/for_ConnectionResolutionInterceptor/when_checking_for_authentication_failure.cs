// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ConnectionResolutionInterceptor;

public class when_checking_for_authentication_failure : Specification
{
    [Fact] void should_recognize_an_invalid_postgresql_password() =>
        ConnectionResolutionInterceptor.IsAuthenticationFailure(new PostgresException("password authentication failed", "FATAL", "FATAL", "28P01")).ShouldBeTrue();

    [Fact] void should_recognize_a_wrapped_failure() =>
        ConnectionResolutionInterceptor.IsAuthenticationFailure(new InvalidOperationException("Opening failed", new PostgresException("denied", "FATAL", "FATAL", "28000"))).ShouldBeTrue();

    [Fact] void should_not_treat_other_failures_as_authentication() =>
        ConnectionResolutionInterceptor.IsAuthenticationFailure(new PostgresException("database does not exist", "FATAL", "FATAL", "3D000")).ShouldBeFalse();
}
