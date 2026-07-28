// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_string_literal;

/// <summary>
/// Screenplay is line based and the printer never escapes, so a line break inside a string literal ends the
/// construct halfway through and everything after it is read as a new declaration. A validation message, a tag or a
/// description carrying one therefore has to be brought back onto a single line before it is handed over.
/// </summary>
public class with_content_that_would_end_the_line : given.a_naming
{
    string? _withALineBreak;
    string? _withACarriageReturn;
    string? _withATab;
    string? _withOnlyControlCharacters;
    string? _withAPathOnTwoLines;

    void Because()
    {
        _withALineBreak = _naming.ToStringLiteral("First line\nSecond line");
        _withACarriageReturn = _naming.ToStringLiteral("First line\r\nSecond line");
        _withATab = _naming.ToStringLiteral("Name\tis required");
        _withOnlyControlCharacters = _naming.ToStringLiteral("\n\t\r");
        _withAPathOnTwoLines = _naming.ToFilePath("Lending/Reserving\n/Reserving.cs");
    }

    [Fact] void should_bring_a_line_break_onto_one_line() => _withALineBreak.ShouldEqual("First line Second line");
    [Fact] void should_bring_a_carriage_return_onto_one_line() => _withACarriageReturn.ShouldEqual("First line Second line");
    [Fact] void should_bring_a_tab_onto_one_line() => _withATab.ShouldEqual("Name is required");
    [Fact] void should_treat_control_characters_only_as_absent() => _withOnlyControlCharacters.ShouldBeNull();
    [Fact] void should_bring_a_path_onto_one_line() => _withAPathOnTwoLines.ShouldEqual("Lending/Reserving /Reserving.cs");
}
