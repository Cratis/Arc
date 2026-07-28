// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Screenplay knows exactly two constraint rules - a property of an event has to be unique, and an event may occur
/// once. Reading the chain recovers both of them from the code that declares them.
/// </summary>
public class a_constraint_declared_in_code : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Events.Constraints;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [EventType]
        public record AuthorRetired(string Name);

        public class AuthorConstraints : IConstraint
        {
            public void Define(IConstraintBuilder builder)
            {
                builder.Unique(_ => _.WithName("UniqueAuthorName").On<AuthorRegistered>(e => e.Name));
                builder.Unique<AuthorRetired>("Only one retirement");
            }
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ConstraintModel Constraint(string name) => _analysis.Slice().Constraints.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_the_rule_on_a_property() => Constraint("UniqueAuthorName").ShouldBeOfExactType<UniquePropertyConstraintModel>();
    [Fact] void should_name_the_property_it_constrains() => ((UniquePropertyConstraintModel)Constraint("UniqueAuthorName")).Property.ShouldEqual("Name");
    [Fact] void should_name_the_event_it_constrains() => ((UniquePropertyConstraintModel)Constraint("UniqueAuthorName")).EventName.ShouldEqual("AuthorRegistered");
    [Fact] void should_recover_the_rule_on_a_whole_event() => _analysis.Slice().Constraints.OfType<UniqueEventConstraintModel>().Single().EventName.ShouldEqual("AuthorRetired");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
