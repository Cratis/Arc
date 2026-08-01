// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_string_literal;

/// <summary>
/// The Screenplay printer never escapes string literals, so a quote reaching it produces output that cannot be
/// parsed back. Anything the printer cannot represent has to be neutralized before it is handed over.
/// </summary>
public class with_content_the_printer_cannot_escape : given.a_naming
{
    string? _withAQuote;
    string? _withWhitespaceOnly;
    string? _withNull;
    string? _withSurroundingWhitespace;

    void Because()
    {
        _withAQuote = _naming.ToStringLiteral(@"He said ""hello""");
        _withWhitespaceOnly = _naming.ToStringLiteral("   ");
        _withNull = _naming.ToStringLiteral(null);
        _withSurroundingWhitespace = _naming.ToStringLiteral("  Registers an author.  ");
    }

    [Fact] void should_replace_every_quote() => _withAQuote.ShouldEqual("He said 'hello'");
    [Fact] void should_treat_whitespace_only_as_absent() => _withWhitespaceOnly.ShouldBeNull();
    [Fact] void should_treat_null_as_absent() => _withNull.ShouldBeNull();
    [Fact] void should_trim_surrounding_whitespace() => _withSurroundingWhitespace.ShouldEqual("Registers an author.");
}
