// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_property_name;

/// <summary>
/// The Screenplay property line regex is <c>^[a-z_]\w*$</c>, so every declared PascalCase name has to be lower
/// camelled before it is emitted.
/// </summary>
public class from_declared_names : given.a_naming
{
    string _pascalCased;
    string _acronym;
    string _allUpperCase;
    string _startingWithADigit;
    string _alreadyCamelCased;
    string _empty;

    void Because()
    {
        _pascalCased = _naming.ToPropertyName("AuthorName");
        _acronym = _naming.ToPropertyName("ISBNValue");
        _allUpperCase = _naming.ToPropertyName("ISBN");
        _startingWithADigit = _naming.ToPropertyName("1Value");
        _alreadyCamelCased = _naming.ToPropertyName("name");
        _empty = _naming.ToPropertyName(string.Empty);
    }

    [Fact] void should_lower_camel_a_pascal_cased_name() => _pascalCased.ShouldEqual("authorName");
    [Fact] void should_keep_the_word_boundary_of_an_acronym() => _acronym.ShouldEqual("isbnValue");
    [Fact] void should_lower_an_entirely_upper_case_name() => _allUpperCase.ShouldEqual("isbn");
    [Fact] void should_prefix_a_name_starting_with_a_digit() => _startingWithADigit.ShouldEqual("_1Value");
    [Fact] void should_leave_an_already_camel_cased_name_alone() => _alreadyCamelCased.ShouldEqual("name");
    [Fact] void should_fall_back_for_a_name_with_nothing_in_it() => _empty.ShouldEqual("value");
}
