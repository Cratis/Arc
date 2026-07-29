// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_declaration_name;

/// <summary>
/// A runtime name is idiomatically written with separators - a Chronicle constraint named <c>unique-timesheet-start</c>
/// is the common case - and a Screenplay identifier cannot hold one. The separators mark word boundaries, so the
/// segments they mark are PascalCased and joined rather than run together into an unreadable identifier. A name that
/// already looks like an identifier carries no separator and is left exactly as it was.
/// </summary>
public class from_names_carrying_separators : given.a_naming
{
    string _kebabCased;
    string _snakeCased;
    string _spaceSeparated;
    string _alreadyPascalCased;
    string _acronym;

    void Because()
    {
        _kebabCased = _naming.ToDeclarationName("unique-timesheet-start");
        _snakeCased = _naming.ToDeclarationName("unique_invitation_email");
        _spaceSeparated = _naming.ToDeclarationName("unique timesheet start");
        _alreadyPascalCased = _naming.ToDeclarationName("TimesheetStarted");
        _acronym = _naming.ToDeclarationName("ISBNValue");
    }

    [Fact] void should_pascal_case_a_kebab_cased_name() => _kebabCased.ShouldEqual("UniqueTimesheetStart");
    [Fact] void should_pascal_case_a_snake_cased_name() => _snakeCased.ShouldEqual("UniqueInvitationEmail");
    [Fact] void should_pascal_case_a_space_separated_name() => _spaceSeparated.ShouldEqual("UniqueTimesheetStart");
    [Fact] void should_leave_an_already_pascal_cased_name_alone() => _alreadyPascalCased.ShouldEqual("TimesheetStarted");
    [Fact] void should_leave_an_acronym_without_separators_alone() => _acronym.ShouldEqual("ISBNValue");
}
