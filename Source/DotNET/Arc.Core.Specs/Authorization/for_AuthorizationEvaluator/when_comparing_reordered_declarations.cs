// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_comparing_reordered_declarations : Specification
{
    static readonly AuthorizationRequirement _roles = new(["Admin", "Editor"]);
    static readonly AuthorizationRequirement _other = new(["Reader"]);
    static readonly AuthorizationDeclaration _checked = new(false, true, [_roles, _other, _roles]);

    [Fact] void should_accept_reordered_roles_and_requirements() =>
        AuthorizationEvaluator.SameDeclaration(_checked, Declared(_other, new AuthorizationRequirement(["Editor", "Admin"]), _roles)).ShouldBeTrue();

    [Fact] void should_accept_duplicated_roles() =>
        AuthorizationEvaluator.SameDeclaration(Declared(_other), Declared(new AuthorizationRequirement(["Reader", "Reader"]))).ShouldBeTrue();

    [Fact] void should_deny_changed_role_membership() =>
        AuthorizationEvaluator.SameDeclaration(_checked, Declared(new AuthorizationRequirement(["Editor", "Other"]), _other, _roles)).ShouldBeFalse();

    [Fact] void should_deny_a_missing_duplicate_requirement() =>
        AuthorizationEvaluator.SameDeclaration(_checked, Declared(_roles, _other, _other)).ShouldBeFalse();

    [Fact] void should_keep_policy_order_across_requirements_significant_because_it_selects_the_primary_identity() =>
        AuthorizationEvaluator.SameDeclaration(
            Declared(new AuthorizationRequirement([]) { Policy = "PolicyA" }, new AuthorizationRequirement([]) { Policy = "PolicyB" }),
            Declared(new AuthorizationRequirement([]) { Policy = "PolicyB" }, new AuthorizationRequirement([]) { Policy = "PolicyA" })).ShouldBeFalse();

    [Fact] void should_keep_scheme_order_significant_because_it_selects_the_primary_identity() =>
        AuthorizationEvaluator.SameDeclaration(
            Declared(new AuthorizationRequirement([]) { AuthenticationSchemes = ["First", "Second"] }),
            Declared(new AuthorizationRequirement([]) { AuthenticationSchemes = ["Second", "First"] })).ShouldBeFalse();

    [Fact] void should_keep_scheme_order_across_reordered_requirements_significant() =>
        AuthorizationEvaluator.SameDeclaration(
            Declared(new AuthorizationRequirement([]) { AuthenticationSchemes = ["First"] }, new AuthorizationRequirement([]) { AuthenticationSchemes = ["Second"] }),
            Declared(new AuthorizationRequirement([]) { AuthenticationSchemes = ["Second"] }, new AuthorizationRequirement([]) { AuthenticationSchemes = ["First"] })).ShouldBeFalse();

    static AuthorizationDeclaration Declared(params AuthorizationRequirement[] requirements) => new(false, true, requirements);
}
