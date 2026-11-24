// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_mixed_anonymous_and_protected_arc_policies : Specification
{
    [Theory]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"Protected\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize]")]
    [InlineData("[Authorize(Policy = \"Anyone\", Roles = \"Staff\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\"), Roles(\"Staff\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\", AuthenticationSchemes = \"Bearer\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"Unknown\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"StaffOnly\")]")]
    public void should_refuse_the_document_instead_of_rewriting_a_compound_anonymous_gate(string authorization)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.All(authorization: authorization));
        AnonymousPolicySources.AssertRefused(result);
        result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
        result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
        result.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
    }

    [Theory]
    [InlineData("[Authorize]")]
    [InlineData("[Roles(\"Staff\")]")]
    [InlineData("[Authorize(AuthenticationSchemes = \"Bearer\")]")]
    public void should_refuse_inherited_requirements_without_modifying_the_policy(string authorization)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.WithInheritedAuthorization(authorization));
        AnonymousPolicySources.AssertRefused(result);
        result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Fact]
    public void should_refuse_a_read_model_type_gate_alongside_its_anonymous_method_policy()
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.All(readModelAuthorization: "[Authorize]"));
        AnonymousPolicySources.AssertRefused(result);
        result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
        result.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    public void should_refuse_two_distinct_anonymous_policy_references()
    {
        var sources = AnonymousPolicySources.All(
            authorization: "[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"Other\")]",
            extraRegistrations: "services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Other\", true);");
        var result = AnonymousPolicySources.Generate(sources);
        AnonymousPolicySources.AssertRefused(result);
        result.Model.Policies.Where(policy => policy.Requirement is AnonymousRequirement).Select(policy => policy.Name).ShouldContainOnly(["Anyone", "Other"]);
    }

    [Theory]
    [InlineData("[Authorize(Policy = \"Anyone\", Roles = \"Anyone\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\"), Roles(\"Anyone\")]")]
    public void should_not_let_a_policy_declaration_suppress_a_role_with_the_same_name(string authorization)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.All(authorization: authorization));
        AnonymousPolicySources.AssertRefused(result);
        result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
        result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.Roles.ShouldContainOnly(["Anyone"]);
    }

    [Theory]
    [InlineData("[Authorize]", false)]
    [InlineData("[Authorize]", true)]
    [InlineData("[Authorize(Roles = \"Staff\")]", false)]
    [InlineData("[Authorize(Roles = \"Staff\")]", true)]
    [InlineData("[Authorize(AuthenticationSchemes = \"Bearer\")]", false)]
    [InlineData("[Authorize(AuthenticationSchemes = \"Bearer\")]", true)]
    [InlineData("[Authorize(Policy = \"Unknown\")]", false)]
    [InlineData("[Authorize(Policy = \"Unknown\")]", true)]
    [InlineData("[Authorize(Policy = \"StaffOnly\")]", false)]
    [InlineData("[Authorize(Policy = \"StaffOnly\")]", true)]
    public void should_preserve_anonymous_denial_for_non_anonymous_protected_controls(string authorization, bool query)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.All(authorization: authorization));
        result.Verification!.Compiles.ShouldBeTrue();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        var run = AnonymousPolicySources.Run(result.Model, query, authenticated: false, denied: true);
        run.Passed.ShouldBeTrue();
        Assert.IsType<SemanticRejected>(run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    }
}
