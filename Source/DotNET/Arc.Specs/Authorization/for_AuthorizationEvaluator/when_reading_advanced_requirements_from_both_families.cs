// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_reading_advanced_requirements_from_both_families : given.both_attribute_families
{
    AuthorizationDeclaration _arcFirst;
    AuthorizationDeclaration _aspNetFirst;
    AuthorizationDeclaration _overridden;
    AuthorizationDeclaration _overriddenAspNetFirst;
    AuthorizationDeclaration _anonymous;
    AuthorizationDeclaration _anonymousArcFirst;

    void Because()
    {
        _arcFirst = ArcDeclarationsFirst().For(typeof(MixedPolicies));
        _aspNetFirst = AspNetDeclarationsFirst().For(typeof(MixedPolicies));
        _overridden = ArcDeclarationsFirst().For(typeof(MixedPolicies).GetMethod(nameof(MixedPolicies.OnlyMethodPolicy))!);
        _overriddenAspNetFirst = AspNetDeclarationsFirst().For(typeof(MixedPolicies).GetMethod(nameof(MixedPolicies.OnlyMethodPolicy))!);
        _anonymous = AspNetDeclarationsFirst().For(typeof(MixedPolicies).GetMethod(nameof(MixedPolicies.Public))!);
        _anonymousArcFirst = ArcDeclarationsFirst().For(typeof(MixedPolicies).GetMethod(nameof(MixedPolicies.Public))!);
    }

    [Fact] void should_collect_every_named_policy_with_arc_first() =>
        _arcFirst.Requirements.Select(requirement => requirement.Policy).Order().ShouldContainOnly(["ArcPolicy", "AspPolicy"]);
    [Fact] void should_collect_every_named_policy_with_aspnet_first() =>
        _aspNetFirst.Requirements.Select(requirement => requirement.Policy).Order().ShouldContainOnly(["ArcPolicy", "AspPolicy"]);
    [Fact] void should_preserve_the_explicit_scheme_in_either_order() =>
        _aspNetFirst.Requirements.SelectMany(requirement => requirement.AuthenticationSchemes).ShouldContainOnly(["Special"]);
    [Fact] void should_keep_roles_inside_one_attribute_as_alternatives() =>
        _arcFirst.Requirements.Single(requirement => requirement.Policy == "ArcPolicy").AnyOfRoles.ShouldContainOnly(["Admin", "Manager"]);
    [Fact] void should_replace_type_policies_on_the_method_with_arc_first() =>
        _overridden.Requirements.Select(requirement => requirement.Policy).ShouldContainOnly(["MethodPolicy"]);
    [Fact] void should_replace_type_policies_on_the_method_with_aspnet_first() =>
        _overriddenAspNetFirst.Requirements.Select(requirement => requirement.Policy).ShouldContainOnly(["MethodPolicy"]);
    [Fact] void should_allow_a_method_to_override_the_protected_type_with_aspnet_first() => _anonymous.AllowsAnonymous.ShouldBeTrue();
    [Fact] void should_allow_a_method_to_override_the_protected_type_with_arc_first() => _anonymousArcFirst.AllowsAnonymous.ShouldBeTrue();

    [Authorize(Policy = "ArcPolicy", Roles = "Admin,Manager")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspPolicy", AuthenticationSchemes = "Special")]
    public static class MixedPolicies
    {
        [Authorize(Policy = "MethodPolicy")]
        public static void OnlyMethodPolicy()
        {
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public static void Public()
        {
        }
    }
}
