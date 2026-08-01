// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_declaration_name;

/// <summary>
/// A runtime name is routinely written with separators - kebab case is idiomatic for a Chronicle constraint - and the
/// Screenplay identifier rules force some transformation of it. Deleting the separators throws away the word
/// boundaries the source stated, so each one is read as the boundary it is instead.
/// </summary>
public class from_names_carrying_separators : given.a_naming
{
    string _kebabCased;
    string _snakeCased;
    string _spaced;
    string _dotted;
    string _mixed;
    string _leadingSeparator;
    string _withoutASeparator;
    string _alreadyCamelCased;
    string _nothingButSeparators;

    void Because()
    {
        _kebabCased = _naming.ToDeclarationName("unique-timesheet-start");
        _snakeCased = _naming.ToDeclarationName("unique_invitation_email");
        _spaced = _naming.ToDeclarationName("Only one retirement");
        _dotted = _naming.ToDeclarationName("library.authors.registered");
        _mixed = _naming.ToDeclarationName("unique invitation-email.address");
        _leadingSeparator = _naming.ToDeclarationName("-overdue");
        _withoutASeparator = _naming.ToDeclarationName("AuthorRegistered");
        _alreadyCamelCased = _naming.ToDeclarationName("authorRegistered");
        _nothingButSeparators = _naming.ToDeclarationName("---");
    }

    [Fact] void should_pascal_case_a_kebab_cased_name() => _kebabCased.ShouldEqual("UniqueTimesheetStart");
    [Fact] void should_pascal_case_a_snake_cased_name() => _snakeCased.ShouldEqual("UniqueInvitationEmail");
    [Fact] void should_pascal_case_a_name_written_as_words() => _spaced.ShouldEqual("OnlyOneRetirement");
    [Fact] void should_pascal_case_a_dotted_name() => _dotted.ShouldEqual("LibraryAuthorsRegistered");
    [Fact] void should_read_every_kind_of_separator_in_one_name() => _mixed.ShouldEqual("UniqueInvitationEmailAddress");
    [Fact] void should_leave_the_casing_of_one_word_alone() => _leadingSeparator.ShouldEqual("overdue");
    [Fact] void should_leave_a_name_carrying_no_separator_exactly_as_it_is() => _withoutASeparator.ShouldEqual("AuthorRegistered");
    [Fact] void should_not_raise_a_name_that_carries_no_separator() => _alreadyCamelCased.ShouldEqual("authorRegistered");
    [Fact] void should_fall_back_for_a_name_with_no_word_in_it() => _nothingButSeparators.ShouldEqual("_");
}
