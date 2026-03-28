// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilder.when_selecting;

public class and_second_collection_changes : given.a_joined_observe_builder
{
    List<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B)> _emissions = [];
    ISubject<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B)> _subject;

    async Task Because()
    {
        var firstEmit = new TaskCompletionSource();
        var secondEmit = new TaskCompletionSource();
        _subject = _builder.Select((a, b) => (a, b));
        _subject.Subscribe(result =>
        {
            _emissions.Add(result);
            if (_emissions.Count == 1) firstEmit.TrySetResult();
            else secondEmit.TrySetResult();
        });

        await firstEmit.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _databaseChanges.OnNext(CreateChangeForCollection(DatabaseName, Collection2Name));
        await secondEmit.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_emit_twice() => _emissions.Count.ShouldEqual(2);

    [Fact] void should_include_first_collection_documents_in_second_emission() =>
        _emissions[1].A.ShouldContainOnly(_docs1);

    [Fact] void should_include_second_collection_documents_in_second_emission() =>
        _emissions[1].B.ShouldContainOnly(_docs2);
}
