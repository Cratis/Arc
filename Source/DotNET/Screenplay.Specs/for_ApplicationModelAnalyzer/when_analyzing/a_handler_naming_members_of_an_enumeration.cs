// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The compiler hands a constant of an enumeration over as the number behind the member, so a mapping and a guard
/// both arrive as numbers unless the type they were written as is read alongside them. A document saying
/// <c>role = 6</c> while the concept it refers to declares <c>clientContact</c> describes nothing anyone can follow.
/// </summary>
public class a_handler_naming_members_of_an_enumeration : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Access.Inviting;

        public enum UserRole
        {
            None,
            CustomerAdvisor,
            ClientContact
        }

        [EventType]
        public record ClientContactInvited(UserRole Role, int Attempt);

        [EventType]
        public record AccessGranted(UserRole Role);

        [Command]
        public record InviteUser(UserRole Role)
        {
            public object Handle()
            {
                if (Role == UserRole.ClientContact)
                {
                    return new ClientContactInvited(UserRole.ClientContact, 6);
                }

                return new AccessGranted(Role);
            }
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<ProducesModel> _produces;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _produces = _analysis.Slice().Commands.First().Produces;
    }

    ProducesModel Produced(string name) => _produces.First(_ => _.EventName == name);

    MappingSourceModel MappedOnto(string name, string property) => Produced(name).Mappings.First(_ => _.Property == property).Source;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_name_the_member_a_mapping_is_given() => MappedOnto("ClientContactInvited", "Role").ShouldEqual(new LiteralSource(new EnumValue("ClientContact")));
    [Fact] void should_name_the_member_a_guard_compares_against() => Produced("ClientContactInvited").When.ShouldEqual(new ComparisonCondition("Role", ComparisonKind.Equal, new LiteralSource(new EnumValue("ClientContact"))));
    [Fact] void should_name_the_member_the_opposite_guard_compares_against() => Produced("AccessGranted").When.ShouldEqual(new ComparisonCondition("Role", ComparisonKind.NotEqual, new LiteralSource(new EnumValue("ClientContact"))));
    [Fact] void should_leave_a_number_belonging_to_no_enumeration_as_a_number() => MappedOnto("ClientContactInvited", "Attempt").ShouldEqual(new LiteralSource(6));
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
