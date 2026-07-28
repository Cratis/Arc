// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_AuthorizeSyntaxBuilder.when_building;

/// <summary>
/// Roles are alternatives to each other while a named policy is an additional demand, and the well known
/// authenticated policy stands in only when nothing else was asked for. Adding it alongside a role would quietly
/// turn "any librarian" into "any librarian who is also authenticated by name", which is a different rule.
/// </summary>
public class what_a_command_requires_of_the_caller : Specification
{
    AuthorizeSyntaxBuilder _builder;
    AuthorizeSyntax? _withRoles;
    AuthorizeSyntax? _withNothingNamed;
    AuthorizeSyntax? _withNothingRequired;

    void Establish() => _builder = new();

    void Because()
    {
        _withRoles = _builder.Build(new AuthorizationModel(true, ["Librarian", "Archivist"]));
        _withNothingNamed = _builder.Build(new AuthorizationModel(true, []));
        _withNothingRequired = _builder.Build(null);
    }

    [Fact] void should_reference_every_role_it_was_given() => _withRoles!.Policies.Select(_ => _.Name).ShouldEqual(["Archivist", "Librarian"]);
    [Fact] void should_offer_the_roles_as_alternatives() => _withRoles!.Policies.Select(_ => _.IsAlternative).ShouldEqual([false, true]);
    [Fact] void should_not_add_authentication_alongside_a_role() => _withRoles!.Policies.Select(_ => _.Name).ShouldNotContain(AuthorizeSyntaxBuilder.AuthenticatedPolicy);
    [Fact] void should_stand_in_with_authentication_when_nothing_was_named() => _withNothingNamed!.Policies.Select(_ => _.Name).ShouldEqual([AuthorizeSyntaxBuilder.AuthenticatedPolicy]);
    [Fact] void should_build_nothing_when_nothing_is_required() => _withNothingRequired.ShouldBeNull();
    [Fact] void should_record_every_policy_the_document_has_to_declare() => _builder.Referenced.ShouldContainOnly(["Archivist", "Librarian", AuthorizeSyntaxBuilder.AuthenticatedPolicy]);
}
