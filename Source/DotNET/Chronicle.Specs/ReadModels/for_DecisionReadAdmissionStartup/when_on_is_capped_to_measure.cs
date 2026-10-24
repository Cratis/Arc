// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_on_is_capped_to_measure : given.a_startup
{
    void Establish()
    {
        _settings["ReadProtection:MaximumMode"] = "Measure";
        Configure(typeof(WrappedOnCommand), typeof(OnCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_only_warn_for_the_wrapped_read() => Logged(LogLevel.Warning, typeof(WrappedOnCommand).FullName!).ShouldBeTrue();
    [Fact] void should_not_check_plain_reads_outside_on() => Logged(LogLevel.Warning, typeof(OnCommand).FullName!).ShouldBeFalse();
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
}
