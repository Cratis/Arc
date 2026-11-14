// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_parsing_a_generated_document_with_an_unknown_event : given.a_document_with_an_unpopulated_field
{
    EventModelView _view;
    EventModelDiagnostic[] _errors;
    string _source;
    EventModelParser _parser;

    void Establish()
    {
        _source = Source.Replace("from RowAdded", "from MissingEvent", StringComparison.Ordinal);
        var compiler = new ScreenplayCompiler();
        _errors = [.. compiler.Compile(_source).Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).Select(EventModelDiagnostic.From)];
        _parser = new EventModelParser(compiler);
    }

    void Because() => _view = _parser.Parse("Records", "Records", _source, EventModelOrigin.Generated);

    [Fact] void should_exercise_an_error_other_than_coverage() => Array.Exists(_errors, _ => _.Code != "PLAY0284").ShouldBeTrue();
    [Fact] void should_refuse_the_document() => _view.Success.ShouldBeFalse();
    [Fact] void should_not_draw_the_document() => _view.EventModel.ShouldBeNull();
    [Fact] void should_keep_all_compiler_errors() => _view.Errors.ShouldEqual<EventModelDiagnostic>(_errors);
    [Fact] void should_not_demote_coverage_when_other_errors_exist() => _view.Warnings.Select(_ => _.Code).ShouldNotContain("PLAY0284");
}
