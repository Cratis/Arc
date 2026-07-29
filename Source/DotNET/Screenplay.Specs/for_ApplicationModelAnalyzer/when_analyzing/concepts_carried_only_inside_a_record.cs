// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A record is referred to by name and never declared, so nothing inside it is ever named on its own. Collecting only
/// the types written straight onto an artifact therefore lost every concept reached through one - and a concept marked
/// as personal data lost that way leaves a document understating what the application holds about people, which is the
/// one thing declaring concepts is most for. A concept can be declared wherever it was reached from, so it is.
/// </summary>
public class concepts_carried_only_inside_a_record : Specification
{
    const string Source = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : ConceptAs<Guid>(Value);

        [PII("The name of a person")]
        public record FirstName(string Value) : ConceptAs<string>(Value);

        public record MentorNote(string Value) : ConceptAs<string>(Value);

        public record ShelfCode(string Value) : ConceptAs<string>(Value);

        public enum ContactPreference { Email, Phone }

        public record PersonalDetails(FirstName First, ContactPreference Preference);

        public record Mentorship(MentorNote Note, Mentorship? Next);

        [EventType]
        public record AuthorRegistered(AuthorId Id, PersonalDetails Details, IEnumerable<Mentorship> Mentors);

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public ShelfCode Shelf { get; init; } = new(string.Empty);

            public static IEnumerable<Author> All() => [];
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ConceptModel Concept(string name) => _analysis.Model.Concepts.First(_ => _.Name == name);

    IEnumerable<ScreenplayDiagnostic> Shapes =>
        _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableShape);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_declare_a_concept_carried_inside_a_record() => Concept("FirstName").Primitive.ShouldEqual(ScreenplayPrimitive.String);
    [Fact] void should_keep_what_that_concept_says_about_personal_data() => Concept("FirstName").IsPii.ShouldBeTrue();
    [Fact] void should_declare_an_enumeration_carried_inside_a_record() => Concept("ContactPreference").EnumValues.ShouldContainOnly(["Email", "Phone"]);
    [Fact] void should_declare_a_concept_carried_inside_a_record_a_collection_holds() => Concept("MentorNote").Primitive.ShouldEqual(ScreenplayPrimitive.String);
    [Fact] void should_declare_a_concept_only_a_read_model_carries() => Concept("ShelfCode").Primitive.ShouldEqual(ScreenplayPrimitive.String);
    [Fact] void should_walk_a_record_referring_to_itself_only_once() => _analysis.Model.Concepts.Select(_ => _.Name).ShouldContainOnly(["AuthorId", "ContactPreference", "FirstName", "MentorNote", "ShelfCode"]);
    [Fact] void should_say_the_shape_of_a_record_a_property_carries_is_not_declared() => Shapes.Count().ShouldEqual(2);
    [Fact] void should_name_the_records_it_could_not_declare() => Shapes.All(_ => _.Message.Contains("PersonalDetails", StringComparison.Ordinal) || _.Message.Contains("Mentorship", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_say_it_of_a_read_model_the_slice_describes() => Shapes.Any(_ => _.Message.Contains("Author'", StringComparison.Ordinal)).ShouldBeFalse();
}
