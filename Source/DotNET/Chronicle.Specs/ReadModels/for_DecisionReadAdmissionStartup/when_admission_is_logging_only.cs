// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_admission_is_logging_only : given.a_startup
{
    void Establish()
    {
        _settings["ReadProtection:StartupAdmissionCheck"] = "lOg";
        Configure(typeof(OnCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_log_the_refusal() => Logged(LogLevel.Critical, "Hierarchy").ShouldBeTrue();
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
}
