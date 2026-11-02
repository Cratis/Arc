// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_reading_stated_lists;

public class with_an_extra_trailing_item : Specification
{
    const string Source = """
        given
            tags
                item "urgent"
                item "review"
                item "unexpected"
            requester = "Jane Austen"
        then
            tags none
        """;

    IReadOnlyList<IReadOnlyList<string>> _lists;

    void Because() => _lists = StatedLists.Of(Source, "tags");

    [Fact] void should_read_every_stated_list() => _lists.Count.ShouldEqual(2);
    [Fact] void should_keep_the_extra_item_and_end_the_block_at_the_next_sibling() => _lists[0].SequenceEqual(StatedLists.Quoted("urgent", "review", "unexpected")).ShouldBeTrue();
    [Fact] void should_not_match_the_expected_list() => _lists[0].SequenceEqual(StatedLists.Quoted("urgent", "review")).ShouldBeFalse();
    [Fact] void should_read_none_as_an_empty_list() => _lists[1].ShouldBeEmpty();
}
