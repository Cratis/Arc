// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilderWithThreeCollections.when_selecting;

public class and_no_changes_occur : given.a_joined_observe_builder
{
    List<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B, IEnumerable<DocumentC> C)> _emissions = [];
    ISubject<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B, IEnumerable<DocumentC> C)> _subject;

    async Task Because()
    {
        var firstEmit = new TaskCompletionSource();
        _subject = _builder.Select((a, b, c) => (a, b, c));
        _subject.Subscribe(result =>
        {
            _emissions.Add(result);
            firstEmit.TrySetResult();
        });

        await firstEmit.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_emit_once() => _emissions.Count.ShouldEqual(1);

    [Fact] void should_include_first_collection_documents() =>
        _emissions[0].A.ShouldContainOnly(_docs1);

    [Fact] void should_include_second_collection_documents() =>
        _emissions[0].B.ShouldContainOnly(_docs2);

    [Fact] void should_include_third_collection_documents() =>
        _emissions[0].C.ShouldContainOnly(_docs3);
}
