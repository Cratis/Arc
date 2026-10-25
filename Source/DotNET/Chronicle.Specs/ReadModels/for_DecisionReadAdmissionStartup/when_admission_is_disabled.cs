// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_admission_is_disabled : given.a_startup
{
    void Establish()
    {
        _settings["ReadProtection:StartupAdmissionCheck"] = "oFf";
        Configure(typeof(OnCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_not_acquire_a_reader() => _startupReads.Requests.ShouldEqual(0);
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
}
