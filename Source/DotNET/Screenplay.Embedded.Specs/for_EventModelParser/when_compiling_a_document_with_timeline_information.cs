// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_compiling_a_document_with_timeline_information : Specification
{
    EventModelView _view;
    bool _hasTimelineInformation;

    void Because()
    {
        var source = string.Join(
            '\n',
            "domain Library",
            "module Catalog",
            "  feature Authors",
            "    slice StateView Listing",
            "      readmodel Author",
            "        name String",
            "      projection Author => Author",
            "        automap",
            "        from AuthorRegistered",
            "    slice StateChange Registration",
            "      event AuthorRegistered",
            "        name String");
        var compiler = new ScreenplayCompiler();
        _hasTimelineInformation = compiler.Compile(source).Diagnostics.Any(_ => _.Code == "PLAY0516" && _.Severity == DiagnosticSeverity.Information);
        _view = new EventModelParser(compiler).Parse("Library", "Library", source);
    }

    [Fact] void should_exercise_a_compiler_information_diagnostic() => _hasTimelineInformation.ShouldBeTrue();
    [Fact] void should_compile_the_board_document() => _view.Success.ShouldBeTrue();
    [Fact] void should_not_surface_information_as_a_board_warning() => _view.Warnings.ShouldBeEmpty();
    [Fact] void should_not_surface_information_as_a_board_error() => _view.Errors.ShouldBeEmpty();
}
