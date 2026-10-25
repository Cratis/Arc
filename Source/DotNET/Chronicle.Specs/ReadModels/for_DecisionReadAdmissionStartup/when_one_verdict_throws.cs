// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_one_verdict_throws : given.a_startup
{
    void Establish()
    {
        _reader.Describe<Balance>().Returns(_ => throw new InvalidOperationException("Metadata unavailable"));
        Configure(typeof(OnCommand), typeof(StreamCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_warn() => Logged(LogLevel.Warning, "could not complete").ShouldBeTrue();
    [Fact] void should_continue_to_other_models() => _reader.Received(1).AdmitForStream<StreamBalance>();
    [Fact] void should_not_treat_the_exception_as_a_refusal() => _lifetime.DidNotReceive().StopApplication();
}
