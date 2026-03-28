// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilder.when_selecting;

public class and_unrelated_collection_changes : given.a_joined_observe_builder
{
    List<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B)> _emissions = [];
    ISubject<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B)> _subject;

    async Task Because()
    {
        var firstEmit = new TaskCompletionSource();
        _subject = _builder.Select((a, b) => (a, b));
        _subject.Subscribe(result =>
        {
            _emissions.Add(result);
            firstEmit.TrySetResult();
        });

        await firstEmit.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _databaseChanges.OnNext(CreateChangeForCollection(DatabaseName, "some_unrelated_collection"));
        await Task.Delay(TimeSpan.FromMilliseconds(200));
    }

    [Fact] void should_emit_only_once() => _emissions.Count.ShouldEqual(1);
}
