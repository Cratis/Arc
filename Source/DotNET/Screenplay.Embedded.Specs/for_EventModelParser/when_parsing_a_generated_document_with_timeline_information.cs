// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_parsing_a_generated_document_with_timeline_information : Specification
{
    EventModelView _view;
    bool _hasTimelineInformation;

    void Because()
    {
        const string source =
            """
            domain Library
            module Catalog
              feature Authors
                slice StateView Listing
                  readmodel Author
                    name String
                  projection Author => Author
                    automap
                    from AuthorRegistered
                slice StateChange Registration
                  event AuthorRegistered
                    name String
            """;
        var compiler = new ScreenplayCompiler();
        _hasTimelineInformation = compiler.Compile(source).Diagnostics.Any(_ => _.Code == "PLAY0516" && _.Severity == DiagnosticSeverity.Information);
        _view = new EventModelParser(compiler).Parse("Library", "Library", source, EventModelOrigin.Generated);
    }

    [Fact] void should_exercise_a_compiler_information_diagnostic() => _hasTimelineInformation.ShouldBeTrue();
    [Fact] void should_draw_the_document() => _view.EventModel.ShouldNotBeNull();
    [Fact] void should_succeed() => _view.Success.ShouldBeTrue();
    [Fact] void should_not_surface_information_as_a_warning() => _view.Warnings.ShouldBeEmpty();
    [Fact] void should_not_surface_information_as_an_error() => _view.Errors.ShouldBeEmpty();
}
