// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A policy is named on the artifact and declared where the application is composed. Flattening it to "somebody has
/// to be there" loses the only thing the attribute actually says, so the name survives and the rule behind it is
/// looked up in the registration - a role, a claim, or several of them combined.
/// </summary>
/// <remarks>
/// The authorization framework is declared alongside the application rather than referenced, because the recognizer
/// matches on the names of the types the registration is written against and never on the assembly they came from.
/// </remarks>
public class a_command_authorized_by_a_named_policy : Specification
{
    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(PolicySource.All());

    PolicyModel Policy(string name) => _analysis.Model.Policies.First(_ => _.Name == name);

    CommandModel Command(string name) => _analysis.Model.Slices.SelectMany(_ => _.Commands).First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(PolicySource.All()).ShouldBeEmpty();
    [Fact] void should_carry_the_name_of_the_policy_the_command_names() => Command("ReserveBook").Authorization!.Policies.ShouldContainOnly(["CanReserve"]);
    [Fact] void should_still_require_an_authenticated_caller() => Command("ReserveBook").Authorization!.RequiresAuthentication.ShouldBeTrue();
    [Fact] void should_keep_a_role_apart_from_a_policy() => Command("ForceReturn").Authorization!.Roles.ShouldContainOnly(["Librarian"]);
    [Fact] void should_declare_a_policy_for_every_name_the_application_refers_to() => _analysis.Model.Policies.Select(_ => _.Name).ShouldContainOnly(["CanReserve", "Librarian", "SeniorStaff", "Trusted", "Unregistered"]);
    [Fact] void should_recover_requirements_declared_one_after_the_other_as_all_having_to_hold() => Policy("CanReserve").Requirement.ShouldEqual(new CombinedRequirement(new RoleRequirement("Librarian"), false, new ClaimRequirement("branch", "central")));
    [Fact] void should_recover_the_several_values_of_one_requirement_as_alternatives() => Policy("SeniorStaff").Requirement.ShouldEqual(new CombinedRequirement(new RoleRequirement("Librarian"), true, new RoleRequirement("Archivist")));
    [Fact] void should_recover_a_role_named_by_an_artifact_as_the_policy_it_implies() => Policy("Librarian").Requirement.ShouldEqual(new RoleRequirement("Librarian"));
    [Fact] void should_recover_nothing_from_a_requirement_that_lives_in_code() => Policy("Trusted").Requirement.ShouldBeNull();
    [Fact] void should_recover_nothing_from_a_policy_the_application_never_registers() => Policy("Unregistered").Requirement.ShouldBeNull();
    [Fact] void should_report_both_policies_it_could_not_recover() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable, ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable]);
    [Fact] void should_say_which_requirement_lives_in_code() => _analysis.Diagnostics.Any(_ => _.Message.Contains("'RequireAssertion'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_say_which_policy_is_never_registered() => _analysis.Diagnostics.Any(_ => _.Message.Contains("'Unregistered'", StringComparison.Ordinal)).ShouldBeTrue();
}
