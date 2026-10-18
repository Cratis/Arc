// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypesInvolved;

public class when_removing_duplicates_from_types_added_twice : Specification
{
    TypesInvolved _types;

    void Establish()
    {
        _types = new();
        _types.Add(typeof(string));
        _types.Add(typeof(int));
        _types.Add(typeof(string));
    }

    void Because() => _types.RemoveDuplicates();

    [Fact] void should_keep_each_type_once() => _types.Count.ShouldEqual(2);
    [Fact] void should_keep_the_order_of_first_occurrence() => _types.ShouldContainOnly(typeof(string), typeof(int));
    [Fact] void should_still_contain_the_types() => (_types.Contains(typeof(string)) && _types.Contains(typeof(int))).ShouldBeTrue();
}
