// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_the_reader_is_unavailable : given.a_startup
{
    void Establish()
    {
        _startupReads.GetHandler = _ => Task.FromResult<IDecisionReads?>(null);
        Configure(typeof(OnCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_record_the_skip() => Logged(LogLevel.Debug, "skipped").ShouldBeTrue();
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
}
