// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_a_wrapped_read_is_refused_outside_on : given.a_startup
{
    void Establish() => Configure(typeof(WrappedCommand), typeof(ProtectedCommand));
    Task Because() => StartAndCheck();

    [Fact] void should_only_warn() => Logged(LogLevel.Warning, "Hierarchy").ShouldBeTrue();
    [Fact] void should_leave_the_host_running() => _lifetime.DidNotReceive().StopApplication();
}
