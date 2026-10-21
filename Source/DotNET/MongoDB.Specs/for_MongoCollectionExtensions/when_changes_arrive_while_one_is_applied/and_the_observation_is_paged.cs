// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_changes_arrive_while_one_is_applied;

public class and_the_observation_is_paged : given.a_busy_observation
{
    const int Changes = 50;
    ObservedDocument _changed;
    Emission _afterChanges;
    int _countsAfterChanges;

    void Establish()
    {
        PageByName(1, 2);
        _changed = new(Guid.NewGuid(), "C");
        _documents = [new(Guid.NewGuid(), "A"), new(Guid.NewGuid(), "B"), _changed];
    }

    async Task Because()
    {
        await StartObserving();
        Update(0);
        await Busy();
        for (var change = 1; change < Changes; change++)
        {
            Update(change);
        }

        Release();
        _afterChanges = await NextEmission();
        await Task.Delay(200);
        _countsAfterChanges = Volatile.Read(ref _counts);
    }

    void Update(int change)
    {
        _documents[2] = _changed with { Name = $"C{change:D2}" };
        PushChange(UpdateOf(_documents[2]));
    }

    [Fact] void should_show_the_last_change() => Assert.Equal([$"C{Changes - 1:D2}"], _afterChanges.Page.Select(_ => _.Name));
    [Fact] void should_read_the_page_once_for_the_held_change_and_once_for_all_that_arrived_meanwhile() => _countsAfterChanges.ShouldEqual(3);
}
