// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_injecting_a_stream_keyed_read : given.a_stream_keyed_startup
{
    void Establish() => Configure(typeof(InjectedStreamRead));
    Task Because() => StartAndCheck();

    [Fact] void should_request_static_admission() => _startupReads.Requests.ShouldEqual(1);
    [Fact] void should_report_the_command() => Logged(LogLevel.Critical, typeof(InjectedStreamRead).FullName!).ShouldBeTrue();
    [Fact] void should_report_the_key_only_refusal() => Logged(LogLevel.Critical, nameof(DecisionReadRefusalReason.StreamKeyRequiresStreamScope)).ShouldBeTrue();
    [Fact] void should_stop_the_host() => _lifetime.Received(1).StopApplication();
}
