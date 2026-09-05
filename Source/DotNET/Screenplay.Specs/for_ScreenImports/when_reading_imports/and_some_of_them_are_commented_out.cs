// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.for_ScreenImports.when_reading_imports;

/// <summary>
/// A statement is recognized by starting a line, which a commented one still does. Removing what a comment holds is
/// what tells them apart, and the line breaks the comment spanned have to survive it - joining the line after a block
/// comment to the line before would hide the real import that follows one.
/// </summary>
public class and_some_of_them_are_commented_out : Specification
{
    const string Component = """
        // import { CommentedOnItsOwnLine } from './One';
            //import { IndentedAndCommented } from './Two';
        import { Kept } from './Three'; // import { AfterAStatement } from './Four';
        /* import { CommentedInABlock } from './Five'; */
        /*
        import { CommentedOverSeveralLines } from './Six';
        */
        import { KeptAfterABlock } from './Seven';
        """;

    IReadOnlyCollection<string> _names;

    void Because() => _names = ScreenImports.In(Component);

    [Fact] void should_keep_every_import_the_file_really_makes() => _names.ShouldContainOnly(["Kept", "KeptAfterABlock"]);
}
