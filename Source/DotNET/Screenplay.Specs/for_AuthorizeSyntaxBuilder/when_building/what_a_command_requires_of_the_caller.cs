// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_AuthorizeSyntaxBuilder.when_building;

/// <summary>
/// Roles are alternatives to each other while a named policy is an additional demand, and the well known
/// authenticated policy stands in only when nothing else was asked for. Adding it alongside a role would quietly
/// turn "any librarian" into "any librarian who is also authenticated by name", which is a different rule.
/// </summary>
/// <remarks>
/// The shape of the requirement carries as much as the names in it. <c>and</c> binds tighter than <c>or</c>, so the
/// alternatives have to sit nested under the demand: a flat <c>Archivist or Librarian and SeniorStaff</c> reads back
/// as "any archivist, or anyone who is senior staff", which lets in a caller that neither the roles nor the policy
/// admits on its own. That is why the tree is asserted here branch by branch and not only by the policies it names.
/// </remarks>
public class what_a_command_requires_of_the_caller : Specification
{
    AuthorizeSyntaxBuilder _builder;
    AuthorizeSyntax? _withRoles;
    AuthorizeSyntax? _withRolesAndAPolicy;
    AuthorizeSyntax? _withNothingNamed;
    AuthorizeSyntax? _withNothingRequired;

    void Establish() => _builder = new();

    void Because()
    {
        _withRoles = _builder.Build(new AuthorizationModel(true, ["Librarian", "Archivist"]));
        _withRolesAndAPolicy = _builder.Build(new AuthorizationModel(true, ["Librarian", "Archivist"]) { Policies = ["SeniorStaff"] });
        _withNothingNamed = _builder.Build(new AuthorizationModel(true, []));
        _withNothingRequired = _builder.Build(null);
    }

    [Fact] void should_reference_every_role_it_was_given() => _withRoles!.References().Select(_ => _.Name).ShouldEqual(["Archivist", "Librarian"]);
    [Fact] void should_offer_the_roles_as_alternatives() => Operator(_withRoles!.Requirement).ShouldEqual(LogicalOperator.Or);
    [Fact] void should_not_add_authentication_alongside_a_role() => _withRoles!.References().Select(_ => _.Name).ShouldNotContain(AuthorizeSyntaxBuilder.AuthenticatedPolicy);

    [Fact] void should_demand_the_named_policy_on_top_of_the_roles() => Operator(_withRolesAndAPolicy!.Requirement).ShouldEqual(LogicalOperator.And);
    [Fact] void should_keep_the_roles_alternatives_to_each_other_under_that_demand() => Operator(Left(_withRolesAndAPolicy!.Requirement)).ShouldEqual(LogicalOperator.Or);
    [Fact] void should_hold_every_role_on_the_side_the_demand_is_asked_of() => Names(Left(_withRolesAndAPolicy!.Requirement)).ShouldEqual(["Archivist", "Librarian"]);
    [Fact] void should_ask_for_the_policy_itself_rather_than_as_another_alternative() => Names(Right(_withRolesAndAPolicy!.Requirement)).ShouldEqual(["SeniorStaff"]);
    [Fact] void should_group_the_alternatives_where_the_document_would_otherwise_read_them_apart() =>
        AuthorizeLine(_withRolesAndAPolicy!).ShouldEqual("authorize (Archivist or Librarian) and SeniorStaff");

    [Fact] void should_stand_in_with_authentication_when_nothing_was_named() => _withNothingNamed!.References().Select(_ => _.Name).ShouldEqual([AuthorizeSyntaxBuilder.AuthenticatedPolicy]);
    [Fact] void should_build_nothing_when_nothing_is_required() => _withNothingRequired.ShouldBeNull();
    [Fact] void should_record_every_policy_the_document_has_to_declare() => _builder.Referenced.ShouldContainOnly(["Archivist", "Librarian", "SeniorStaff", AuthorizeSyntaxBuilder.AuthenticatedPolicy]);

    /// <summary>
    /// Prints a document carrying the requirement and hands back the line the caller reads it on.
    /// </summary>
    /// <param name="authorize">The <see cref="AuthorizeSyntax"/> to print.</param>
    /// <returns>The <c>authorize</c> line as it appears in the document.</returns>
    /// <remarks>
    /// The tree is asserted branch by branch above; this asks the language's own printer what that tree says out
    /// loud, which is the thing a reader of the document - and the compiler reading it back - actually goes by.
    /// </remarks>
    static string AuthorizeLine(AuthorizeSyntax authorize)
    {
        var command = new CommandSyntax("ReserveBook", [], authorize, [], [], null, SourceLocation.Start);
        var slice = new SliceSyntax(SliceType.StateChange, "Reserving", [], [command], [], [], [], [], [], [], [], SourceLocation.Start);
        var module = new ModuleSyntax("Library", [], [new FeatureSyntax("Lending", [], [slice], SourceLocation.Start)], SourceLocation.Start);
        var printed = new ScreenplayPrinter().Print(new ApplicationSyntax([], [], [], [module], SourceLocation.Start));

        return printed.Split('\n').Select(_ => _.Trim()).Single(_ => _.StartsWith("authorize ", StringComparison.Ordinal));
    }

    static LogicalOperator Operator(PolicyRequirementSyntax requirement) => ((LogicalPolicyRequirementSyntax)requirement).Operator;

    static PolicyRequirementSyntax Left(PolicyRequirementSyntax requirement) => ((LogicalPolicyRequirementSyntax)requirement).Left;

    static PolicyRequirementSyntax Right(PolicyRequirementSyntax requirement) => ((LogicalPolicyRequirementSyntax)requirement).Right;

    static IEnumerable<string> Names(PolicyRequirementSyntax requirement) =>
        requirement is LogicalPolicyRequirementSyntax logical
            ? Names(logical.Left).Concat(Names(logical.Right))
            : [((PolicyReferenceSyntax)requirement).Name];
}
