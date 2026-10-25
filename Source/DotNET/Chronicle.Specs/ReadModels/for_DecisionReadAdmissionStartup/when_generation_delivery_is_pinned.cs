// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_generation_delivery_is_pinned : given.a_startup
{
    void Establish()
    {
        _reader.Describe<Balance>().Returns(new DecisionReadDescription(new(true, null), [], true));
        Configure(typeof(OnCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_refuse_the_delivery_policy() => Logged(LogLevel.Critical, "GenerationDeliveryNotSupported").ShouldBeTrue();
    [Fact] void should_stop_the_host() => _lifetime.Received(1).StopApplication();
}
