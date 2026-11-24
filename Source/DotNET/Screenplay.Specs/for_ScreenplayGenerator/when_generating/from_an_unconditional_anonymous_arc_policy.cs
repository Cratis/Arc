// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unconditional_anonymous_arc_policy : a_generated_document
{
    void Because() => Generate(AnonymousPolicySources.All());

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_admit_anonymous_and_authenticated_callers_through_the_recovered_gate(bool query, bool authenticated)
    {
        var run = AnonymousPolicySources.Run(Result.Model, query, authenticated, denied: false);
        run.Passed.ShouldBeTrue();
        Assert.IsType<SemanticAccepted>(run.Execution);
    }

    [Fact] void should_keep_null_authorization_open() => new AuthorizeSyntaxBuilder().Build(null).ShouldBeNull();
    [Fact] void should_keep_empty_anonymous_authorization_open() => new AuthorizeSyntaxBuilder().Build(AuthorizationModel.None).ShouldBeNull();
    [Fact] void should_ignore_empty_policy_and_role_names_on_an_open_artifact() => new AuthorizeSyntaxBuilder().Build(new(false, [" "]) { Policies = [""] }).ShouldBeNull();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_a_named_requirement_without_implicit_authentication(bool role)
    {
        var builder = new AuthorizeSyntaxBuilder();
        var model = role ? new AuthorizationModel(false, ["Anyone"]) : new AuthorizationModel(false, []) { Policies = ["Anyone"] };
        var gate = builder.Build(model);
        gate.ShouldNotBeNull();
        Assert.IsType<PolicyReferenceSyntax>(gate!.Requirement).Name.ShouldEqual("Anyone");
        builder.Referenced.ShouldContainOnly(["Anyone"]);
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(AnonymousPolicySources.All()).ShouldBeEmpty();
    [Fact] void should_compile_bind_and_print_stably() => AssertDocument();
    [Fact] void should_keep_the_policy_declaration() => Result.Source.ShouldContain("policy Anyone");
    [Fact] void should_emit_the_unconditional_anonymous_condition() => Result.Source.ShouldContain("require anonymous");
    [Fact] void should_not_introduce_an_authenticated_condition() => Result.Source.ShouldNotContain("require authenticated");
    [Fact] void should_represent_the_recovered_condition_explicitly() => Result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement?.GetType().Name.ShouldEqual("AnonymousRequirement");
    [Fact] void should_keep_the_command_policy_reference() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.Policies.ShouldContainOnly(["Anyone"]);
    [Fact] void should_keep_the_query_policy_reference() => Result.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.Policies.ShouldContainOnly(["Anyone"]);
    [Fact] void should_not_require_authentication_on_the_command() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
    [Fact] void should_not_require_authentication_on_the_query() => Result.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
    [Fact] void should_report_no_unrecoverable_policy() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable).ShouldBeEmpty();
}
