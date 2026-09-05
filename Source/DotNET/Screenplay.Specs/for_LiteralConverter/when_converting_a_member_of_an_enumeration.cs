// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_LiteralConverter;

/// <summary>
/// A concept declares the members of an enumeration in lower camel case and every reference to one is a string
/// matching a declared member exactly. The member therefore goes through the same name conversion the declaration
/// was written with, rather than the number behind it being written as a number nothing declares.
/// </summary>
public class when_converting_a_member_of_an_enumeration : Specification
{
    readonly ScreenplayNaming _naming = new();
    LiteralExpressionSyntax _member;
    LiteralExpressionSyntax _acronym;
    LiteralExpressionSyntax _number;

    void Because()
    {
        _member = LiteralConverter.Convert(new EnumValue("ClientContact"), _naming);
        _acronym = LiteralConverter.Convert(new EnumValue("ISBNOnly"), _naming);
        _number = LiteralConverter.Convert(6, _naming);
    }

    [Fact] void should_write_the_member_in_the_casing_the_concept_declares_it() => _member.Value.ShouldEqual("clientContact");
    [Fact] void should_write_the_member_as_a_string() => _member.Value.ShouldBeOfExactType<string>();
    [Fact] void should_convert_an_acronym_the_way_the_declaration_does() => _acronym.Value.ShouldEqual("isbnOnly");
    [Fact] void should_leave_a_number_that_belongs_to_no_enumeration_as_a_number() => _number.Value.ShouldBeOfExactType<double>();
    [Fact] void should_not_treat_a_member_as_a_number() => LiteralConverter.IsNumeric(new EnumValue("ClientContact")).ShouldBeFalse();
}
