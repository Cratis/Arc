// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_declaration_name;

/// <summary>
/// An accented letter can be written as one character or as a letter followed by a combining mark, and Unicode calls
/// the two canonically equal - a compiler does too, and an editor is free to save either. A combining mark is not a
/// letter though, so composing the name has to happen before anything is stripped from it or one spelling keeps its
/// accent and the other quietly loses it, which is two documents for one application.
/// </summary>
public class from_names_written_two_ways_that_mean_one_thing : given.a_naming
{
    const string Composed = "Andr\u00E9Registered";
    const string Decomposed = "Andre\u0301Registered";

    string _fromTheComposedName;
    string _fromTheDecomposedName;

    void Because()
    {
        _fromTheComposedName = _naming.ToDeclarationName(Composed);
        _fromTheDecomposedName = _naming.ToDeclarationName(Decomposed);
    }

    [Fact] void should_spell_the_two_names_apart_to_begin_with() => Decomposed.ShouldNotEqual(Composed);
    [Fact] void should_read_the_two_spellings_as_one_name() => _fromTheDecomposedName.ShouldEqual(_fromTheComposedName);
    [Fact] void should_keep_the_accent_of_the_composed_spelling() => _fromTheComposedName.ShouldEqual(Composed);
    [Fact] void should_keep_the_accent_of_the_decomposed_spelling() => _fromTheDecomposedName.ShouldEqual(Composed);
}
