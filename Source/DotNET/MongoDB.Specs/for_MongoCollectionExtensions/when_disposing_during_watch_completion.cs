// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_disposing_during_watch_completion : given.a_controlled_watch
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_not_fail_when_the_shared_stream_completes_while_the_observable_is_disposed(string kind)
    {
        await StartObserving(kind);

        // Completion of the shared stream and disposal of the observable race: both end the observation, and
        // neither may be allowed to fault the other or leave the cancellation resources disposed underneath it.
        var completion = Task.Run(_databaseChanges.Complete);
        var disposal = Task.Run(() => ((IDisposable)_observable).Dispose());
        var error = await Catch.Exception(() => Task.WhenAll(completion, disposal).WaitAsync(Deadline));

        error.ShouldBeNull();
        var deadline = DateTime.UtcNow + Deadline;
        while (_databaseChanges.Subscribers > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        _databaseChanges.Subscribers.ShouldEqual(0);
    }
}
