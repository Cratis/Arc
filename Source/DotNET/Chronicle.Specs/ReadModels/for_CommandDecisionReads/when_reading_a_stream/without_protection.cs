// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream;

public class without_protection : given.a_stream_reader
{
    DecisionRead<Model> _second;

    async Task Because()
    {
        Begin(typeof(UnprotectedCommand));
        _read = await _reader.Get<Model>("owner", "reports", "period");
        _second = await _reader.Get<Model>("owner", "reports", "period");
    }

    [Fact] void should_share_the_advisory_read_within_the_invocation() => ReferenceEquals(_read, _second).ShouldBeTrue();
    [Fact] void should_discard_the_guard() => _read.IsProtected.ShouldBeFalse();
    [Fact] void should_keep_the_folded_instance() => ReferenceEquals(_read.Instance, _token.Instance).ShouldBeTrue();
    [Fact] void should_keep_the_stream_projection_key() => _read.Key.ShouldEqual(_token.Key);
    [Fact] void should_not_enroll() => _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    [Fact] Task should_fold_the_stream_once() => _inner.Received(1).GetDetached<Model>("owner", "reports", "period", null, Arg.Any<CancellationToken>());
}
