// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_the_shape_is_admitted : given.a_startup
{
    void Establish()
    {
        _reader.Describe<Balance>().Returns(new DecisionReadDescription(new(true, null), [], false));
        Configure(typeof(OnCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_check_the_shape() => _reader.Received(1).Describe<Balance>();
    [Fact] void should_not_log_a_refusal() => Logged(LogLevel.Critical, typeof(Balance).FullName!).ShouldBeFalse();
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
}
