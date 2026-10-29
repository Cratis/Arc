// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_command_authorized_by_an_arc_policy : Specification
{
    static readonly string[] _unsupported = ["Negated", "ThreeValued", "Opaque", "Weekday", "Async", "Field", "Generic"];
    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(ArcPolicySource.All());

    PolicyModel Policy(string name) => _analysis.Model.Policies.Single(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(ArcPolicySource.All()).ShouldBeEmpty();
    [Fact] void should_recover_authenticated_and_a_role_as_both_having_to_hold() => Policy("AccountantOnly").Requirement.ShouldEqual(new CombinedRequirement(AuthenticatedRequirement.Instance, false, new RoleRequirement("Accountant")));
    [Fact] void should_recover_a_single_role() => Policy("CanComplete").Requirement.ShouldEqual(new RoleRequirement("Automation"));
    [Fact] void should_keep_the_nesting_of_alternatives_and_conjunctions() => Policy("Nested").Requirement.ShouldEqual(new CombinedRequirement(new CombinedRequirement(new RoleRequirement("A"), true, new RoleRequirement("B")), false, new ClaimRequirement("branch", "central")));
    [Fact] void should_recover_a_policy_that_checks_cancellation_first() => Policy("Block").Requirement.ShouldEqual(new RoleRequirement("Clerk"));
    [Fact] void should_recover_a_policy_returning_a_constructed_value_task() => Policy("Constructed").Requirement.ShouldEqual(new RoleRequirement("Auditor"));
    [Fact] void should_recover_an_explicit_interface_implementation() => Policy("Explicit").Requirement.ShouldEqual(new RoleRequirement("Clerk"));
    [Fact] void should_recover_a_policy_registered_through_the_static_extension_call() => Policy("Block").Requirement.ShouldEqual(new RoleRequirement("Clerk"));
    [Fact] void should_recover_a_policy_that_evaluates_anonymous_callers_without_adding_authentication() => Policy("Constructed").Requirement.ShouldEqual(new RoleRequirement("Auditor"));
    [Fact] void should_recover_nothing_from_a_negation() => Policy("Negated").Requirement.ShouldBeNull();
    [Fact] void should_recover_nothing_from_three_valued_helpers() => Policy("ThreeValued").Requirement.ShouldBeNull();
    [Fact] void should_recover_nothing_from_an_opaque_body() => Policy("Opaque").Requirement.ShouldBeNull();
    [Fact] void should_recover_nothing_from_the_receipt_time() => Policy("Weekday").Requirement.ShouldBeNull();
    [Fact] void should_recover_nothing_from_an_asynchronous_body() => Policy("Async").Requirement.ShouldBeNull();
    [Fact] void should_recover_nothing_from_a_role_held_in_a_field() => Policy("Field").Requirement.ShouldBeNull();
    [Fact] void should_recover_nothing_from_a_policy_registered_through_a_type_parameter() => Policy("Generic").Requirement.ShouldBeNull();

    [Fact]
    void should_report_each_policy_it_could_not_recover()
    {
        var diagnostics = _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable).ToArray();
        diagnostics.Length.ShouldEqual(_unsupported.Length);
        foreach (var name in _unsupported)
        {
            diagnostics.Count(_ => _.Message.Contains($"'{name}'", StringComparison.Ordinal)).ShouldEqual(1);
        }
    }

    [Fact]
    void should_report_nothing_for_the_recovered_policies() => _analysis.Diagnostics
        .Where(_ => _.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable)
        .All(diagnostic => _unsupported.Any(name => diagnostic.Message.Contains($"'{name}'", StringComparison.Ordinal)))
        .ShouldBeTrue();
}
