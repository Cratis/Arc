// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_a_read_is_refused : given.a_startup
{
    void Establish() => Configure(typeof(OnCommand), typeof(UnprotectedMethodCommand));
    Task Because() => StartAndCheck();

    [Fact] void should_stop_the_host() => _lifetime.Received(1).StopApplication();
    [Fact] void should_name_the_command() => Logged(LogLevel.Critical, typeof(OnCommand).FullName!).ShouldBeTrue();
    [Fact] void should_name_the_model() => Logged(LogLevel.Critical, typeof(Balance).FullName!).ShouldBeTrue();
    [Fact] void should_name_the_reason() => Logged(LogLevel.Critical, "Hierarchy").ShouldBeTrue();
    [Fact] void should_still_check_parameters_of_an_advisory_method_body() => Logged(LogLevel.Critical, typeof(UnprotectedMethodCommand).FullName!).ShouldBeTrue();
    [Fact] void should_not_materialize_a_read() => _reader.ReceivedCalls().Any(call => call.GetMethodInfo().Name.StartsWith("Get", StringComparison.Ordinal)).ShouldBeFalse();
}
