// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Remarks that would close the documentation fence are reported instead of corrupting the document.
/// </summary>
public class from_event_remarks_containing_a_fence : a_batch_a_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        /// <summary>An author acquired a name.</summary>
        /// <remarks>
        /// ```csharp
        /// var name = "Jane Austen";
        /// ```
        /// </remarks>
        [EventType]
        public record AuthorRegistered(string Name);
        """));

    [Fact] void should_keep_the_summary() => Result.Source.ShouldContain("description \"An author acquired a name.\"");
    [Fact] void should_not_emit_a_nested_fence() => Result.Source.ShouldNotContain("documentation");
    [Fact] void should_report_the_remarks_it_left_out() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventFeatureWithoutCounterpart).ShouldBeTrue();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
