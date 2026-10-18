// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypesInvolved;

public class when_removing_a_type_that_was_added_twice : Specification
{
    TypesInvolved _types;

    void Establish()
    {
        _types = new();
        _types.Add(typeof(string));
        _types.Add(typeof(string));
    }

    void Because() => _types.Remove(typeof(string));

    [Fact] void should_still_contain_the_remaining_occurrence() => _types.Contains(typeof(string)).ShouldBeTrue();
    [Fact] void should_have_one_left() => _types.Count.ShouldEqual(1);
}
