// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unconditional_arc_policy_without_anonymous_opt_in : a_generated_document
{
    const string Registration = "services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\");";

    void Because() => Generate(AnonymousPolicySources.All(registration: Registration));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_deny_an_anonymous_caller_without_registration_opt_in(bool query)
    {
        var run = AnonymousPolicySources.Run(Result.Model, query, authenticated: false, denied: true);
        run.Passed.ShouldBeTrue();
        Assert.IsType<SemanticRejected>(run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(AnonymousPolicySources.All(registration: Registration)).ShouldBeEmpty();
    [Fact] void should_compile_bind_and_print_stably() => AssertDocument();
    [Fact] void should_keep_the_policy_name() => Result.Source.ShouldContain("policy Anyone");
    [Fact] void should_not_infer_an_anonymous_condition_from_a_true_body() => Result.Source.ShouldNotContain("require anonymous");
    [Fact] void should_keep_the_authenticated_fallback() => Result.Source.ShouldContain("require authenticated");
    [Fact] void should_leave_the_condition_unrecovered() => Result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeNull();
    [Fact] void should_require_authentication_on_the_command() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
    [Fact] void should_require_authentication_on_the_query() => Result.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
    [Fact] void should_report_the_unrecoverable_policy() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable).ShouldEqual(1);
}
