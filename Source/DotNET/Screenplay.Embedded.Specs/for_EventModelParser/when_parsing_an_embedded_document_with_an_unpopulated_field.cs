// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_parsing_an_embedded_document_with_an_unpopulated_field : given.a_document_with_an_unpopulated_field
{
    EventModelView _view;
    EventModelView _legacy;

    void Because()
    {
        IEventModelParser parser = new EventModelParser();
        _view = parser.Parse("Records", "Records", Source, EventModelOrigin.Embedded);
        _legacy = parser.Parse("Records", "Records", Source);
    }

    [Fact] void should_refuse_the_document() => _view.Success.ShouldBeFalse();
    [Fact] void should_not_draw_the_document() => _view.EventModel.ShouldBeNull();
    [Fact] void should_report_the_coverage_error() => _view.Errors.Select(_ => _.Code).ShouldContain("PLAY0284");
    [Fact] void should_not_demote_the_error() => _view.Warnings.Select(_ => _.Code).ShouldNotContain("PLAY0284");
    [Fact] void should_keep_the_legacy_overload_strict() => _legacy.Success.ShouldBeFalse();
    [Fact] void should_keep_the_legacy_coverage_error() => _legacy.Errors.ShouldEqual<EventModelDiagnostic>(_view.Errors);
    [Fact] void should_not_draw_the_legacy_document() => _legacy.EventModel.ShouldBeNull();
}
