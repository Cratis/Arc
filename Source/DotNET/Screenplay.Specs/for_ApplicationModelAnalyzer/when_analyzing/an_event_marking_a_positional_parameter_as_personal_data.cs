// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An event is canonically a positional record, and an attribute written on a positional parameter belongs to the
/// parameter rather than to the property it produces. Chronicle reads <c>[PII]</c> from the constructor parameter
/// for exactly that reason, so a document that read only the property would say a value is not sensitive while the
/// runtime encrypts it.
/// </summary>
public class an_event_marking_a_positional_parameter_as_personal_data : Specification
{
    const string Source = """
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorName(string Value) : ConceptAs<string>(Value);

        public record AuthorEmail(string Value) : ConceptAs<string>(Value);

        [EventType]
        public record AuthorRegistered([PII] AuthorName Name, AuthorEmail Email);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    bool IsPii(string name) => _analysis.Model.Concepts.Single(_ => _.Name == name).IsPii;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_mark_the_marked_concept_as_personal_data() => IsPii("AuthorName").ShouldBeTrue();
    [Fact] void should_leave_the_unmarked_concept_alone() => IsPii("AuthorEmail").ShouldBeFalse();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
