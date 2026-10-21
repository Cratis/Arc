// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_changes_arrive_while_one_is_applied;

public class and_one_document_changes_many_times : given.a_busy_observation
{
    const int Changes = 50;
    ObservedDocument _often;
    ObservedDocument _once;
    Emission _afterChanges;
    int _checksAfterChanges;

    void Establish()
    {
        _often = _documents[0];
        _once = _documents[1] with { Name = "Second, changed" };
    }

    async Task Because()
    {
        await StartObserving();
        PushChange(UpdateOf(_often with { Name = "0" }));
        await Busy();
        for (var change = 1; change < Changes; change++)
        {
            PushChange(UpdateOf(_often with { Name = $"{change}" }));
        }

        PushChange(UpdateOf(_once));
        Release();
        await NextEmission();
        await NextEmission();
        _afterChanges = await NextEmission();
        await Task.Delay(200);
        _checksAfterChanges = Volatile.Read(ref _belongsChecks);
    }

    [Fact] void should_hold_the_last_change_to_the_document() => _afterChanges.Page.ShouldContain(_often with { Name = $"{Changes - 1}" });
    [Fact] void should_apply_the_change_to_the_other_document() => _afterChanges.Page.ShouldContain(_once);
    [Fact] void should_check_once_for_the_held_change_and_once_per_document_changed_meanwhile() => _checksAfterChanges.ShouldEqual(3);
}
