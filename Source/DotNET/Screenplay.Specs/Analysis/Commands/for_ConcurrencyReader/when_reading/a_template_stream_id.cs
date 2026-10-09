// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Commands.for_ConcurrencyReader.when_reading;

public class a_template_stream_id : Specification
{
    ConcurrencyModel? _result;
    ScreenplayDiagnostics _diagnostics;

    void Establish() => _diagnostics = new();

    void Because()
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, """
            using Cratis.Chronicle.Events;
            namespace Library;
            [EventStreamType("Approval", concurrency: true)]
            [EventStreamId("{Owner}:{Month}", concurrency: true)]
            public record Approve(string Owner, string Month);
            """));
        _result = ConcurrencyReader.Read(compilation.GetTypeByMetadataName("Library.Approve")!, _diagnostics, Analyzed.SlicePath);
    }

    [Fact] void should_not_emit_a_template_as_a_fixed_stream_id() => _result!.StreamId.ShouldBeNull();
    [Fact] void should_keep_the_fixed_stream_type() => _result!.StreamType.ShouldEqual("Approval");
    [Fact] void should_report_the_property_derived_mapping() => _diagnostics.All.Single().Message.ShouldContain("property-derived");
}
