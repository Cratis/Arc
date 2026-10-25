// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_reading_stream_models : given.a_startup
{
    void Establish() => Configure(typeof(StreamCommand));
    Task Because() => StartAndCheck();

    [Fact] void should_use_stream_admission_once() => _reader.Received(1).AdmitForStream<StreamBalance>();
    [Fact] void should_describe_the_shape_once() => _reader.Received(1).Describe<StreamBalance>();
    [Fact] void should_accept_the_stream_shape() => _lifetime.DidNotReceive().StopApplication();
}
