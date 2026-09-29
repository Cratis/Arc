// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions;

public class when_counting_queryables_of_different_element_types : Specification
{
    IQueryable _numbers;
    IQueryable _words;
    int _numberCount;
    int _wordCount;

    void Establish()
    {
        _numbers = new[] { 1, 2, 3 }.AsQueryable();
        _words = new[] { "one", "two" }.AsQueryable();
    }

    void Because()
    {
        _numberCount = _numbers.Count();
        _wordCount = _words.Count();
    }

    [Fact] void should_count_the_numbers() => _numberCount.ShouldEqual(3);
    [Fact] void should_count_the_words() => _wordCount.ShouldEqual(2);
}
