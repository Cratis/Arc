// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;

namespace Cratis.Arc.Screenplay.for_ScreenReferenceScopes.when_qualifying;

public class without_a_unique_emitted_scope : given.an_application_with_screen_references
{
    void Establish()
    {
        Model = Model with { Slices = [.. Model.Slices, LibraryAuthors.Registration() with { Namespace = "Library.Books.Registration" }] };
        Options = new() { SegmentsToSkip = 2 };
    }

    void Because() => Emit();

    [Fact] void should_report_the_ambiguous_references_and_missing_query() => Emission.Diagnostics.Count(_ => _.Code == "SP0028").ShouldEqual(3);
    [Fact] void should_not_emit_a_blindly_fully_qualified_action() => Emission.Source.Contains("action ", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_emit_the_missing_query_binding() => Emission.Source.Contains("via query ", StringComparison.Ordinal).ShouldBeFalse();
}
