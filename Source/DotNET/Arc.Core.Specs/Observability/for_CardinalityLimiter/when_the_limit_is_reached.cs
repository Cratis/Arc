// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability.for_CardinalityLimiter;

public class when_the_limit_is_reached : Specification
{
    CardinalityLimiter _limiter;
    string _first;
    string _second;
    string _third;
    string _firstAgain;

    void Establish() => _limiter = new(2);

    void Because()
    {
        _first = _limiter.Limit("RegisterAuthor");
        _second = _limiter.Limit("RenameAuthor");
        _third = _limiter.Limit("RemoveAuthor");
        _firstAgain = _limiter.Limit("RegisterAuthor");
    }

    [Fact] void should_let_the_first_value_through() => _first.ShouldEqual("RegisterAuthor");
    [Fact] void should_let_the_second_value_through() => _second.ShouldEqual("RenameAuthor");
    [Fact] void should_fold_a_value_past_the_limit_into_other() => _third.ShouldEqual("_other");
    [Fact] void should_keep_letting_a_value_it_has_seen_through() => _firstAgain.ShouldEqual("RegisterAuthor");
}
