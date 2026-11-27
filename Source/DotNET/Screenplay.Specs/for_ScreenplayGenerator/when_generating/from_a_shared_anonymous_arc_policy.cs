// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_shared_anonymous_arc_policy : Specification
{
    [Theory]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize]")]
    [InlineData("[Authorize(Policy = \"Anyone\", Roles = \"Staff\")]")]
    [InlineData("[Roles(\"Anyone\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\", AuthenticationSchemes = \"Bearer\")]")]
    [InlineData("[Authorize(Policy = \"Anyone\"), Authorize(Policy = \"Unknown\")]")]
    public void should_refuse_a_shared_public_command_and_protected_query_without_rewriting_the_shared_policy(string queryAuthorization)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.Shared(queryAuthorization));
        AnonymousPolicySources.AssertRefused(result);
        result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
        result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
        result.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_refuse_standalone_and_shared_controller_use_of_an_anonymous_policy(bool query, bool shared)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.WithController(query, shared));
        AnonymousPolicySources.AssertRefused(result);
        result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
        var controller = result.Model.Slices.Single(slice => slice.Namespace == "Library.Catalog.ControllerBrowsing");
        controller.Commands.Count().ShouldEqual(query ? 0 : 1);
        controller.Queries.Count().ShouldEqual(query ? 1 : 0);
        if (shared)
        {
            result.Model.Slices.Where(slice => slice.Namespace == "Library.Catalog.Browsing").SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_ordinary_protected_controller_recovery_unchanged(bool query)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.WithController(query, shared: false, policy: "Protected"));
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error)
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message} ({diagnostic.Location})")));
        result.Source.ShouldNotBeEmpty();
        result.Verification!.Compiles.ShouldBeTrue();
        result.Source.ShouldNotContain("require anonymous");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_preserve_public_and_unrelated_protected_policy_semantics_in_the_same_document(bool query, bool authenticated)
    {
        var result = AnonymousPolicySources.Generate(AnonymousPolicySources.Shared("[Authorize(Policy = \"Protected\")]"));
        result.IsSuccess.ShouldBeTrue();
        result.Verification!.Compiles.ShouldBeTrue();
        result.Source.ShouldContain("require anonymous");
        result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
        var denied = query && !authenticated;
        var run = AnonymousPolicySources.Run(result.Model, query, authenticated, denied);
        run.Passed.ShouldBeTrue();
        if (denied)
        {
            Assert.IsType<SemanticRejected>(run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        }
        else
        {
            Assert.IsType<SemanticAccepted>(run.Execution);
        }
    }
}
