// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_property_path;

/// <summary>
/// Every segment of a path is a property name in its own right, so camel casing only the first one leaves the rest
/// failing the property regex.
/// </summary>
public class from_a_dotted_path : given.a_naming
{
    string _nested;
    string _single;
    string _empty;

    void Because()
    {
        _nested = _naming.ToPropertyPath("Lines.Quantity");
        _single = _naming.ToPropertyPath("Name");
        _empty = _naming.ToPropertyPath(string.Empty);
    }

    [Fact] void should_camel_case_every_segment() => _nested.ShouldEqual("lines.quantity");
    [Fact] void should_camel_case_a_single_segment() => _single.ShouldEqual("name");
    [Fact] void should_fall_back_for_a_path_with_nothing_in_it() => _empty.ShouldEqual("value");
}
