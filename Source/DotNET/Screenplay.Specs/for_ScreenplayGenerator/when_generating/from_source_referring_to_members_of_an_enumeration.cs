// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// This is the whole point of naming a member end to end: a document declaring <c>concept UserRole : Enum</c> with
/// <c>clientContact</c> underneath it, and then referring to that member by the number behind it, describes an
/// application nobody can read back. Every place a member can appear - a mapping, a guard, a validation rule and a
/// projection - has to write the name the concept declares, while a number that belongs to no enumeration stays a
/// number.
/// </summary>
public class from_source_referring_to_members_of_an_enumeration : Specification
{
    const string Inviting = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using FluentValidation;

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

        public class InviteUserValidator : CommandValidator<InviteUser>
        {
            public InviteUserValidator()
            {
                RuleFor(_ => _.Role).Equal(UserRole.ClientContact);
            }
        }
        """;

    const string Listing = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Projections.ModelBound;
        using Library.Access.Inviting;

        namespace Library.Access.Listing;

        [ReadModel]
        [FromEvent<ClientContactInvited>]
        public record Invitation
        {
            [SetValue<ClientContactInvited>(UserRole.ClientContact)]
            public UserRole Role { get; init; }

            [SetValue<ClientContactInvited>(6)]
            public int Attempt { get; init; }
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Access/Inviting/Inviting.cs", Inviting),
        ("Library/Access/Listing/Listing.cs", Listing)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    IEnumerable<string> Lines() =>
        _result.Source.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim());

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_declare_the_enumeration_as_a_concept() => Lines().ShouldContain("concept UserRole : Enum");
    [Fact] void should_declare_the_member_the_document_refers_to() => Lines().ShouldContain("clientContact");
    [Fact] void should_write_a_mapping_as_the_member_it_names() => Lines().ShouldContain(@"role = ""clientContact""");
    [Fact] void should_write_the_mapping_of_the_command_and_of_the_projection_alike() => Lines().Count(_ => _ == @"role = ""clientContact""").ShouldEqual(2);
    [Fact] void should_write_a_guard_as_the_member_it_compares_against() => Lines().ShouldContain(@"produces when role == ""clientContact""");
    [Fact] void should_write_the_opposite_guard_as_the_member_too() => Lines().ShouldContain(@"produces when role != ""clientContact""");
    [Fact] void should_write_a_validation_rule_as_the_member_it_compares_against() => Lines().ShouldContain(@"role == ""clientContact""");
    [Fact] void should_never_refer_to_a_member_by_the_number_behind_it() => Lines().ShouldNotContain("role = 2");
    [Fact] void should_never_compare_against_the_number_behind_a_member() => Lines().ShouldNotContain("produces when role == 2");
    [Fact] void should_leave_a_number_belonging_to_no_enumeration_as_a_number() => Lines().ShouldContain("attempt = 6");
    [Fact] void should_report_nothing_as_unmappable() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
