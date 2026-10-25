// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream.given;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDispatch;

public class when_dispatching_an_admission_verdict : Specification
{
    DecisionReaderForSpecs _reads;
    DecisionReadRefusalReason? _global;
    DecisionReadRefusalReason? _stream;

    void Establish()
    {
        _reads = Substitute.For<DecisionReaderForSpecs>();
        _reads.Describe<DispatchedModel>().Returns(new DecisionReadDescription(new(false, DecisionReadRefusalReason.Hierarchy), [], false));
        _reads.AdmitForStream<DispatchedModel>().Returns(new DecisionReadAdmission(true, null));
        ReadModelDispatch.Register<DispatchedModel>();
    }

    void Because()
    {
        _global = DecisionReadVerdict.For(_reads, typeof(DispatchedModel), false);
        _stream = DecisionReadVerdict.For(_reads, typeof(DispatchedModel), true);
    }

    [Fact] void should_use_global_admission_without_stream_scope() => _global.ShouldEqual(DecisionReadRefusalReason.Hierarchy);
    [Fact] void should_use_stream_admission_with_stream_scope() => _stream.ShouldBeNull();
    [Fact] void should_describe_the_registered_model() => _reads.Received(2).Describe<DispatchedModel>();
    [Fact] void should_only_check_stream_admission_for_stream_scope() => _reads.Received(1).AdmitForStream<DispatchedModel>();
}
