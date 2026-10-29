// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications.given;

public class a_rendered_single_event_command : for_ScreenplayGenerator.given.a_generated_document
{
    protected const string StatusField = "static readonly InvoiceStatus _status = InvoiceStatus.Draft;";
    protected const string CompleteAssertion = "[Fact] async Task should_have_appended_status_recorded_with_status() => await _scenario.ShouldHaveAppendedEvent<RecordStatus, StatusRecorded>(_id, @event => @event.Status == _status);";
    protected const string ExistenceAssertion = "[Fact] async Task should_have_appended_status_recorded() => await _scenario.ShouldHaveAppendedEvent<RecordStatus, StatusRecorded>(_id, @event => true);";
    protected static readonly ScreenplayOptions Options = new() { Domain = "Invoices", Module = "Invoices", SegmentsToSkip = 1, AuthoringOnlyConstructs = true };
    protected string Scenario = RenderedEnumCommandSources.Scenario;
    protected string Slice = RenderedEnumCommandSources.Slice;
    protected string Common = RenderedEnumCommandSources.Common;

    protected void GenerateScenario() => Generate(Options, RenderedEnumCommandSources.Sources(Scenario, Slice, Common));
}
