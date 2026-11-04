// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableAsyncEnumerator.when_enumerating;

public class and_concurrent_emissions_occur : Specification
{
    Subject<int> _subject;
    ObservableAsyncEnumerator<int> _enumerator;
    List<int> _collectedItems;

    void Establish()
    {
        _subject = new();
        _enumerator = new(_subject, CancellationToken.None);
        _collectedItems = [];
    }

    async Task Because()
    {
        var emit = Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                _subject.OnNext(i);

                // Widen the interleaving window between producer and consumer.
                await Task.Yield();
            }

            _subject.OnCompleted();
        },
        TestContext.Current.CancellationToken);

        var consume = Task.Run(async () =>
        {
            while (await _enumerator.MoveNextAsync())
            {
                _collectedItems.Add(_enumerator.Current);
            }
        },
        TestContext.Current.CancellationToken);

        await Task.WhenAll(emit, consume).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact] void should_not_lose_items_when_emitted_rapidly() => _collectedItems.ShouldEqual(Enumerable.Range(0, 50));

    async Task Destroy()
    {
        await _enumerator.DisposeAsync();
        _subject.Dispose();
    }
}
