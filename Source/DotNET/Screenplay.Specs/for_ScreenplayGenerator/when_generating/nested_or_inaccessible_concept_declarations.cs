// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class nested_or_inaccessible_concept_declarations : a_generated_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Concepts;

        namespace A.Helpers
        {
            public class Service
            {
                public enum Details { Unknown, Known }
                public record NestedCode(string Value) : ConceptAs<string>(Value);
            }
            internal record HiddenCode(string Value) : ConceptAs<string>(Value);
            internal enum HiddenState { Unknown, Known }
            file record FileCode(string Value) : ConceptAs<string>(Value);
        }
        namespace Library.Authors.Listing
        {
            public record Details(string Name);
            [ReadModel]
            public record Entry(string EntryId, Details Detail)
            {
                public static IEnumerable<Entry> AllEntries() => [];
            }
        }
        """;

    void Because() => Generate((Analyzed.SlicePath, Source));

    [Fact] void should_not_recover_unused_nested_or_inaccessible_declarations() => Result.Model.Concepts.ShouldBeEmpty();
    [Fact] void should_keep_the_referenced_details_record() => Result.Model.Types.Single().Name.ShouldEqual("Details");
    [Fact] void should_keep_its_properties() => Result.Model.Types.Single().Properties.Single().Name.ShouldEqual("Name");
    [Fact] void should_keep_the_read_model_name() => Result.Model.Slices.SelectMany(slice => slice.ReadModels).Single().Name.ShouldEqual("Entry");
    [Fact] void should_report_no_lost_reference() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_bind_the_record_reference() => AssertDocument();
}
