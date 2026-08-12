// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A projection setting a property to a constant reaches the document through a mini language, and a member of an
/// enumeration is written into it as a member rather than as a constant - a constant is read back as whichever kind
/// its text looks like, and a member is always the name the concept declares.
/// </summary>
public class a_projection_setting_a_member_of_an_enumeration : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Access.Listing;

        public enum UserRole
        {
            None,
            CustomerAdvisor,
            ClientContact
        }

        [EventType]
        public record ClientContactInvited(string Subject);

        [ReadModel]
        [FromEvent<ClientContactInvited>]
        public record Invitation
        {
            [SetFrom<ClientContactInvited>("subject")]
            public string Subject { get; init; } = string.Empty;

            [SetValue<ClientContactInvited>(UserRole.ClientContact)]
            public UserRole Role { get; init; }

            [SetValue<ClientContactInvited>(6)]
            public int Attempt { get; init; }
        }
        """;

    ApplicationModelAnalysis _analysis;
    ProjectionFromModel _from;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _from = _analysis.Slice().Projections.Single().Scope.From.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_name_the_member_the_property_is_set_to() => _from.Properties["Role"].ShouldEqual("$enum(clientContact)");
    [Fact] void should_leave_a_number_belonging_to_no_enumeration_as_a_number() => _from.Properties["Attempt"].ShouldEqual("$value(6)");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
