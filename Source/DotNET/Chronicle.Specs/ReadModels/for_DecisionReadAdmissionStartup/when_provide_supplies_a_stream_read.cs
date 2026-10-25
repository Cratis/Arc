// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_provide_supplies_a_stream_read : given.a_stream_keyed_startup
{
    void Establish() => Configure(typeof(ProvidedStreamRead));
    Task Because() => StartAndCheck();

    [Fact] void should_have_a_shape_refused_by_key_only_admission() => _admissions.Describe<StreamKeyedBalance>().Admission.Reason.ShouldEqual(DecisionReadRefusalReason.StreamKeyRequiresStreamScope);
    [Fact] void should_have_a_shape_admitted_with_stream_scope() => _admissions.AdmitForStream<StreamKeyedBalance>().IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_request_static_admission_for_the_provided_token() => _startupReads.Requests.ShouldEqual(0);
    [Fact] void should_not_report_a_refusal() => Logged(LogLevel.Critical, typeof(ProvidedStreamRead).FullName!).ShouldBeFalse();
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
}
