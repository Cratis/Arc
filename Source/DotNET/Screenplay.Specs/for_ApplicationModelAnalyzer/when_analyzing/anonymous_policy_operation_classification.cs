// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class anonymous_policy_operation_classification : Specification
{
    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.Source(AnonymousPolicySources.All());

    [Fact] void should_keep_a_named_gate_for_the_command() => _analysis.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.Policies.ShouldContainOnly(["Anyone"]);
    [Fact] void should_keep_a_named_gate_for_the_query() => _analysis.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.Policies.ShouldContainOnly(["Anyone"]);
    [Fact] void should_classify_the_command_before_emission() => _analysis.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
    [Fact] void should_classify_the_query_before_emission() => _analysis.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();

    [Theory]
    [InlineData(AnonymousPolicySources.Body)]
    [InlineData("=> new(true);")]
    [InlineData("=> new ValueTask<bool>(true);")]
    [InlineData("{ return ValueTask.FromResult(true); }")]
    [InlineData("{ cancellationToken.ThrowIfCancellationRequested(); return new(true); }")]
    public void should_recover_only_a_complete_supported_true_body(string body)
    {
        var analysis = AnalyzeBody(body);
        AssertAuthentication(analysis.Model, false);
        analysis.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement?.GetType().Name.ShouldEqual("AnonymousRequirement");
        analysis.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("=> ValueTask.FromResult(false);")]
    [InlineData("=> ValueTask.FromResult(Accept());")]
    [InlineData("=> ValueTask.FromResult(context.Principal.Identity?.IsAuthenticated == true || true);")]
    [InlineData("{ Accept(); return ValueTask.FromResult(true); }")]
    [InlineData("{ if (context.Principal.Identity?.IsAuthenticated == true) return ValueTask.FromResult(true); return ValueTask.FromResult(false); }")]
    [InlineData("=> new ValueTask<bool>(Task.FromResult(true));")]
    public void should_keep_unsupported_or_partial_bodies_protected(string body) => AssertUnsupportedBody(body);

    [Fact]
    public void should_keep_an_async_true_body_protected() => AssertUnsupportedBody("{ await Task.Yield(); return true; }", "async");

    [Theory]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\");", true)]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", evaluatesAnonymous: false);", true)]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", evaluatesAnonymous: anonymous);", true)]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", true);", false)]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(evaluatesAnonymous: true, name: \"Anyone\");", false)]
    [InlineData("AuthorizationPolicyServiceCollectionExtensions.AddArcAuthorizationPolicy<AnyonePolicy>(services, \"Anyone\", true);", false)]
    public void should_require_positive_semantic_evidence_of_anonymous_opt_in(string registration, bool requiresAuthentication)
    {
        var sources = AnonymousPolicySources.All(registration: registration);
        var analysis = Analyzed.Source(sources);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        AssertAuthentication(analysis.Model, requiresAuthentication);
        analysis.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable).ShouldEqual(requiresAuthentication);
    }

    [Theory]
    [InlineData("[Authorize]", true)]
    [InlineData("[Authorize, Authorize(Policy = \"Anyone\")]", true)]
    [InlineData("[Authorize(Policy = \"Anyone\", Roles = \"Staff\")]", true)]
    [InlineData("[Authorize(Policy = \"Anyone\", AuthenticationSchemes = \"Bearer\")]", true)]
    [InlineData("[Authorize(Policy = \"Anyone\"), Roles(\"Staff\")]", true)]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"Protected\")]", true)]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"StaffOnly\")]", true)]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"Unknown\")]", true)]
    [InlineData("[Authorize(Policy = \"anyone\")]", true)]
    [InlineData("[Authorize(Policy = \"AllowAnonymous\")]", true)]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"Other\")]", false)]
    public void should_remove_authentication_only_when_every_requirement_is_proven_anonymous(string authorization, bool requiresAuthentication)
    {
        var sources = AnonymousPolicySources.All(authorization: authorization, extraRegistrations: "services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Other\", true);");
        var analysis = Analyzed.Source(sources);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        AssertAuthentication(analysis.Model, requiresAuthentication);
    }

    [Theory]
    [InlineData("[Authorize]")]
    [InlineData("[Authorize(Policy = \"Protected\")]")]
    [InlineData("[Authorize(Roles = \"Staff\")]")]
    [InlineData("[Authorize(AuthenticationSchemes = \"Bearer\")]")]
    public void should_keep_the_read_model_type_requirements_alongside_a_method_policy(string authorization)
    {
        var sources = AnonymousPolicySources.All(readModelAuthorization: authorization);
        var analysis = Analyzed.Source(sources);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        analysis.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
        analysis.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_use_the_whole_application_independently_of_project_order(bool reverse)
    {
        var policies = Analyzed.Project("Library.Policies", [], ("Library/Policies.cs", AnonymousPolicySources.Policies()));
        var operations = Analyzed.Project("Library.Operations", [], Analyzed.Root, (Analyzed.SlicePath, AnonymousPolicySources.Operations()));
        var composition = Analyzed.Project("Library.Host", [policies.ToMetadataReference()], ("Library/Composition.cs", AnonymousPolicySources.Composition()));
        Analyzed.ErrorsIn(policies).ShouldBeEmpty();
        Analyzed.ErrorsIn(operations).ShouldBeEmpty();
        Analyzed.ErrorsIn(composition).ShouldBeEmpty();
        var analysis = reverse ? Analyzed.Projects(composition, operations, policies) : Analyzed.Projects(policies, operations, composition);
        AssertAuthentication(analysis.Model, false);
    }

    [Theory]
    [InlineData("[Authorize]")]
    [InlineData("[Roles(\"Staff\")]")]
    [InlineData("[Authorize(AuthenticationSchemes = \"Bearer\")]")]
    public void should_not_relax_an_inherited_requirement(string authorization)
    {
        var sources = AnonymousPolicySources.WithInheritedAuthorization(authorization);
        var analysis = Analyzed.Source(sources);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        AssertAuthentication(analysis.Model, true);
    }

    [Theory]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", true);")]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"anyone\", true);")]
    [InlineData("services.AddArcAuthorizationPolicy<ProtectedPolicy>(anonymous.ToString());")]
    [InlineData("new Microsoft.AspNetCore.Authorization.AuthorizationOptions().AddPolicy(\"Anyone\", policy => policy.RequireAuthenticatedUser());")]
    public void should_refuse_an_ambiguous_policy_name_even_when_the_first_registration_is_anonymous(string registration)
    {
        var sources = AnonymousPolicySources.All(extraRegistrations: registration);
        var analysis = Analyzed.Source(sources);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        AssertAuthentication(analysis.Model, true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_detect_case_collisions_across_application_projects_in_either_order(bool reverse)
    {
        var policies = Analyzed.Project("Library.Policies", [], ("Library/Policies.cs", AnonymousPolicySources.Policies()));
        var operations = Analyzed.Project("Library.Operations", [], Analyzed.Root, (Analyzed.SlicePath, AnonymousPolicySources.Operations()));
        var reference = policies.ToMetadataReference();
        var composition = Analyzed.Project("Library.Host", [reference], ("Library/Composition.cs", AnonymousPolicySources.Composition()));
        var conflict = Analyzed.Project("Library.OtherHost", [reference], ("Library/OtherComposition.cs", AnonymousPolicySources.Composition("services.AddArcAuthorizationPolicy<ProtectedPolicy>(\"anyone\");")));
        Analyzed.ErrorsIn(conflict).ShouldBeEmpty();
        var analysis = reverse ? Analyzed.Projects(conflict, composition, operations, policies) : Analyzed.Projects(policies, operations, composition, conflict);
        AssertAuthentication(analysis.Model, true);
    }

    [Fact]
    public void should_not_apply_the_model_bound_exception_to_controller_operations()
    {
        var sources = new[]
        {
            ("Library/Policies.cs", AnonymousPolicySources.Policies()),
            ("Library/Composition.cs", AnonymousPolicySources.Composition()),
            ("Library/Controllers.cs", AnonymousPolicySources.Controllers)
        };
        var analysis = Analyzed.Source(sources);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        AssertAuthentication(analysis.Model, true);
    }

    [Fact]
    public void should_preserve_an_explicit_anonymous_bypass_without_a_policy_gate()
    {
        var analysis = Analyzed.Source(AnonymousPolicySources.All(authorization: "[Authorize(Policy = \"Anyone\"), AllowAnonymous]"));
        analysis.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization.ShouldBeNull();
        analysis.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization.ShouldBeNull();
    }

    [Fact]
    public void should_keep_the_public_authorization_reader_conservative_without_application_evidence()
    {
        var compilation = Analyzed.Compile(AnonymousPolicySources.All());
        var command = compilation.GetTypeByMetadataName("Library.Catalog.Browsing.RegisterInterest")!;
        AuthorizationReader.Read(command)!.RequiresAuthentication.ShouldBeTrue();
    }

    static ApplicationModelAnalysis AnalyzeBody(string body, string methodModifier = "")
    {
        var sources = AnonymousPolicySources.WithBody(body, methodModifier);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        return Analyzed.Source(sources);
    }

    static void AssertUnsupportedBody(string body, string methodModifier = "")
    {
        var analysis = AnalyzeBody(body, methodModifier);
        AssertAuthentication(analysis.Model, true);
        analysis.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeNull();
        analysis.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable).ShouldEqual(1);
    }

    static void AssertAuthentication(ApplicationModel model, bool expected)
    {
        model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldEqual(expected);
        model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldEqual(expected);
    }
}
