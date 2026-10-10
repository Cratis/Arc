// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_command_with_a_template_concurrency_stream_id : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Inventory.Adding;

        [EventType]
        public record BookAdded(string Title);

        [Command]
        [EventStreamType("Inventory", concurrency: true)]
        [EventStreamId("{Owner}:{Month}", concurrency: true)]
        public record AddBook(EventSourceId BookId, string Owner, string Month, string Title)
        {
            public BookAdded Handle() => new(Title);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.Source(Source);

    [Fact] void should_leave_the_property_derived_stream_id_in_code() => _analysis.Slice().Commands.Single().Concurrency!.StreamId.ShouldBeNull();
    [Fact] void should_keep_the_fixed_stream_type() => _analysis.Slice().Commands.Single().Concurrency!.StreamType.ShouldEqual("Inventory");
    [Fact] void should_report_the_omitted_mapping_in_normal_analysis() => _analysis.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Message.ShouldContain("property-derived");
    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
}
