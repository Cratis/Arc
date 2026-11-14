// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_parsing_a_generated_document_with_an_unpopulated_field : given.a_document_with_an_unpopulated_field
{
    EventModelView _view;
    EventModelDiagnostic _coverage;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _coverage = EventModelDiagnostic.From(compiler.Compile(Source).Diagnostics.Single(_ => _.Code == "PLAY0284"));
        _view = new EventModelParser(compiler).Parse("Records", "Records", Source, EventModelOrigin.Generated);
    }

    [Fact] void should_succeed() => _view.Success.ShouldBeTrue();
    [Fact] void should_draw_the_document() => _view.EventModel.ShouldNotBeNull();
    [Fact] void should_not_report_errors() => _view.Errors.ShouldBeEmpty();
    [Fact] void should_keep_the_coverage_code_message_and_location_as_a_warning() => _view.Warnings.ShouldContain(_coverage);
    [Fact] void should_preserve_mapping_warnings() => _view.Warnings.Select(_ => _.Code).ShouldContain(EventModelWarningCodes.ApplicationDeclarationNotCarried);
}
