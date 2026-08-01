// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;

namespace Cratis.Arc.Screenplay.for_LiteralConverter;

/// <summary>
/// The printer never escapes a string literal, so a quote reaching it produces output that cannot be parsed back.
/// </summary>
public class when_converting_a_string : Specification
{
    readonly ScreenplayNaming _naming = new();

    [Fact] void should_neutralize_a_quote() => LiteralConverter.Convert(@"He said ""hi""", _naming).Value.ShouldEqual("He said 'hi'");
    [Fact] void should_keep_a_bool_as_a_bool() => LiteralConverter.Convert(true, _naming).Value.ShouldEqual(true);
    [Fact] void should_keep_nothing_as_nothing() => LiteralConverter.Convert(null, _naming).Value.ShouldBeNull();
}
