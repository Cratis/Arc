// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphDatabaseName.when_getting_the_name;

public class exceeding_the_longest_name : Specification
{
    string _exactlyLongest;
    string _tooLong;

    void Because()
    {
        _exactlyLongest = GraphDatabaseName.For(null, "Orders", new string('x', 56));
        _tooLong = GraphDatabaseName.For(null, "Orders", new string('x', 60));
    }

    [Fact] void should_keep_a_name_of_63_characters() => _exactlyLongest.ShouldEqual($"orders-{new string('x', 56)}");
    [Fact] void should_truncate_to_54_and_append_the_hash_of_the_whole_name() => _tooLong.ShouldEqual($"orders-{new string('x', 47)}-1e73c0a9");
    [Fact] void should_be_63_characters() => _tooLong.Length.ShouldEqual(63);
}
