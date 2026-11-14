// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class a_child_projection_with_context_and_joins : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, ChildJoinSources.Source));

    [Fact] void should_emit_the_context_mapping() => Result.Source.ShouldContain("approvedBy = $eventContext.causedBy.subject");
    [Fact] void should_emit_the_explicit_child_join() => Result.Source.ShouldContain("join unit on codeId");
    [Fact] void should_emit_the_automatically_mapped_child_join() => Result.Source.ShouldContain("join codeId on codeId");
    [Fact] void should_not_declare_the_computed_property() => Result.Source.ShouldNotContain("streamId");
    [Fact] void should_keep_stored_properties() => Result.Source.ShouldContain("approvedBy String");
    [Fact] void should_not_drop_the_mapping() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnmappableProjectionExpression);
    [Fact] void should_not_drop_the_joins() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnmappableProjectionJoin);
    [Fact] void should_compile_and_round_trip() => AssertPartialDocument();
}
