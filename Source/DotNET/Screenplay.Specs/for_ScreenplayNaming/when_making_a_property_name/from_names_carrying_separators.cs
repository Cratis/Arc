// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_property_name;

/// <summary>
/// Reading separators as word boundaries is what every identifier position gets, not only the ones a constraint is
/// named in. A property line is lower camel cased on top of that, which is the same transformation a name written
/// with words would be given by hand.
/// </summary>
public class from_names_carrying_separators : given.a_naming
{
    string _kebabCased;
    string _snakeCased;
    string _leadingUnderscore;
    string _startingWithADigitAfterASeparator;
    string _nothingButSeparators;

    void Because()
    {
        _kebabCased = _naming.ToPropertyName("unique-timesheet-start");
        _snakeCased = _naming.ToPropertyName("first_name");
        _leadingUnderscore = _naming.ToPropertyName("_isbn");
        _startingWithADigitAfterASeparator = _naming.ToPropertyName("first-1st");
        _nothingButSeparators = _naming.ToPropertyName("---");
    }

    [Fact] void should_lower_camel_a_kebab_cased_name() => _kebabCased.ShouldEqual("uniqueTimesheetStart");
    [Fact] void should_lower_camel_a_snake_cased_name() => _snakeCased.ShouldEqual("firstName");
    [Fact] void should_drop_a_leading_underscore() => _leadingUnderscore.ShouldEqual("isbn");
    [Fact] void should_join_a_word_starting_with_a_digit() => _startingWithADigitAfterASeparator.ShouldEqual("first1st");
    [Fact] void should_fall_back_for_a_name_with_no_word_in_it() => _nothingButSeparators.ShouldEqual("value");
}
