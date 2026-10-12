// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream;

public class with_separate_cache_dimensions : given.a_stream_reader
{
    DecisionRead<Model> _wholeSource;
    DecisionRead<Model> _otherStream;
    DecisionRead<Model> _otherType;
    DecisionRead<Model> _otherSourceType;
    DecisionRead<Model> _cached;

    void Establish()
    {
        _inner.GetDetached<Model>((ReadModelKey)"owner", Arg.Any<CancellationToken>()).Returns(DecisionFixtures.Protected<Model>("owner"));
        _inner.GetDetached<Model>("owner", "reports", "other", null, Arg.Any<CancellationToken>()).Returns(DecisionFixtures.Protected<Model>("owner"));
        _inner.GetDetached<Model>("owner", "another-type", "period", null, Arg.Any<CancellationToken>()).Returns(DecisionFixtures.Protected<Model>("owner"));
        _inner.GetDetached<Model>("owner", "reports", "period", "source-type", Arg.Any<CancellationToken>()).Returns(DecisionFixtures.Protected<Model>("owner"));
    }

    async Task Because()
    {
        CommandTransaction.Current = _unit;
        Begin(typeof(ProtectedCommand));
        _wholeSource = await _reader.Get<Model>((ReadModelKey)"owner");
        _read = await _reader.Get<Model>("owner", "reports", "period");
        _otherStream = await _reader.Get<Model>("owner", "reports", "other");
        _otherType = await _reader.Get<Model>("owner", "another-type", "period");
        _otherSourceType = await _reader.Get<Model>("owner", "reports", "period", "source-type");
        _cached = await _reader.Get<Model>("owner", "reports", "period");
    }

    [Fact] void should_not_reuse_the_whole_source_read() => ReferenceEquals(_read, _wholeSource).ShouldBeFalse();
    [Fact] void should_not_reuse_another_stream_id() => ReferenceEquals(_read, _otherStream).ShouldBeFalse();
    [Fact] void should_not_reuse_another_stream_type() => ReferenceEquals(_read, _otherType).ShouldBeFalse();
    [Fact] void should_not_reuse_another_source_type() => ReferenceEquals(_read, _otherSourceType).ShouldBeFalse();
    [Fact] void should_reuse_identical_stream_dimensions() => ReferenceEquals(_read, _cached).ShouldBeTrue();
    [Fact] Task should_fold_identical_stream_dimensions_once() => _inner.Received(1).GetDetached<Model>("owner", "reports", "period", null, Arg.Any<CancellationToken>());
}
