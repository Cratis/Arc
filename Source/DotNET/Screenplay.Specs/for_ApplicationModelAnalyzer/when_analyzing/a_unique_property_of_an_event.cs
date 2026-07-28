// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The shortest way to say a property has to be unique is to say so on the property, and it is the shape Screenplay
/// has an exact counterpart for.
/// </summary>
public class a_unique_property_of_an_event : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Events.Constraints;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered([property: Unique("UniqueAuthorName")] string Name);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_the_constraint() => _analysis.Slice().Constraints.Single().ShouldBeOfExactType<UniquePropertyConstraintModel>();
    [Fact] void should_name_it_what_the_source_named_it() => _analysis.Slice().Constraints.Single().Name.ShouldEqual("UniqueAuthorName");
    [Fact] void should_constrain_the_property_it_was_declared_on() => ((UniquePropertyConstraintModel)_analysis.Slice().Constraints.Single()).Property.ShouldEqual("Name");
    [Fact] void should_constrain_it_on_the_event_declaring_it() => ((UniquePropertyConstraintModel)_analysis.Slice().Constraints.Single()).EventName.ShouldEqual("AuthorRegistered");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
