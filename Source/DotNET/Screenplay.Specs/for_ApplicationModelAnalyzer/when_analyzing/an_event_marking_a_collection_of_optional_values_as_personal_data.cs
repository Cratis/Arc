// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A concept is declared once and referenced by name, so a value marked as personal data has to be marked under the
/// name it is referenced under. A collection of an optional value says one thing about the value and two about how
/// many there are and whether it may be absent - strip fewer of them than the reference does and the mark lands on
/// the name of the wrapper, leaving a document that says a value is not sensitive while the runtime encrypts it.
/// </summary>
public class an_event_marking_a_collection_of_optional_values_as_personal_data : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        public enum AuthorStanding
        {
            Member,
            Honorary
        }

        public enum AuthorTier
        {
            Standard,
            Premium
        }

        [EventType]
        public record AuthorRegistered([PII] IEnumerable<AuthorStanding?> Standings, IEnumerable<AuthorTier?> Tiers);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    bool IsPii(string name) => _analysis.Model.Concepts.Single(_ => _.Name == name).IsPii;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_mark_the_value_the_collection_holds() => IsPii("AuthorStanding").ShouldBeTrue();
    [Fact] void should_leave_the_unmarked_value_alone() => IsPii("AuthorTier").ShouldBeFalse();
    [Fact] void should_declare_a_concept_for_nothing_the_reference_stripped() => _analysis.Model.Concepts.Select(_ => _.Name).ShouldContainOnly(["AuthorStanding", "AuthorTier"]);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
