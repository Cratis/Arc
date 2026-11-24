// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_clashing_anonymous_arc_policy_names : Specification
{
    [Theory]
    [InlineData("Anyone", true)]
    [InlineData("anyone", true)]
    [InlineData(" Anyone ", true)]
    [InlineData("Anyone", false)]
    [InlineData("anyone", false)]
    [InlineData(" Anyone ", false)]
    public void should_refuse_conflicting_registrations_in_either_order(string conflictingName, bool anonymousFirst)
    {
        var conflict = $"services.AddArcAuthorizationPolicy<ProtectedPolicy>(\"{conflictingName}\");";
        var sources = AnonymousPolicySources.All(
            registration: anonymousFirst ? AnonymousPolicySources.Registration : conflict,
            extraRegistrations: anonymousFirst ? conflict : AnonymousPolicySources.Registration);
        AssertRefusedWithoutRewriting(sources);
    }

    [Theory]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", true);")]
    [InlineData("services.AddArcAuthorizationPolicy<AnyonePolicy>(\"anyone\", true);")]
    [InlineData("services.AddArcAuthorizationPolicy<ProtectedPolicy>(anonymous.ToString());")]
    [InlineData("new Microsoft.AspNetCore.Authorization.AuthorizationOptions().AddPolicy(\"Anyone\", policy => policy.RequireAuthenticatedUser());")]
    [InlineData("new Microsoft.AspNetCore.Authorization.AuthorizationOptions().AddPolicy(\"anyone\", policy => policy.RequireAuthenticatedUser());")]
    public void should_not_emit_an_ambiguous_or_unresolved_registration_as_an_executable_fallback(string registration) =>
        AssertRefusedWithoutRewriting(AnonymousPolicySources.All(extraRegistrations: registration));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_collisions_across_projects_in_either_project_order(bool reverse)
    {
        var policies = Analyzed.Project("Library.Policies", [], ("Library/Policies.cs", AnonymousPolicySources.Policies()));
        var operations = Analyzed.Project("Library.Operations", [], Analyzed.Root, (Analyzed.SlicePath, AnonymousPolicySources.Operations()));
        var reference = policies.ToMetadataReference();
        var composition = Analyzed.Project("Library.Host", [reference], ("Library/Composition.cs", AnonymousPolicySources.Composition()));
        var conflict = Analyzed.Project("Library.OtherHost", [reference], ("Library/OtherComposition.cs", AnonymousPolicySources.Composition("services.AddArcAuthorizationPolicy<ProtectedPolicy>(\"anyone\");")));
        Analyzed.ErrorsIn(policies).ShouldBeEmpty();
        Analyzed.ErrorsIn(operations).ShouldBeEmpty();
        Analyzed.ErrorsIn(composition).ShouldBeEmpty();
        Analyzed.ErrorsIn(conflict).ShouldBeEmpty();
        var compilations = reverse ? new[] { conflict, composition, operations, policies } : new[] { policies, operations, composition, conflict };
        var result = new ScreenplayGenerator().Generate(compilations, new());
        AnonymousPolicySources.AssertRefused(result, AnonymousPolicySources.Ambiguous);
    }

    static void AssertRefusedWithoutRewriting((string Path, string Text)[] sources)
    {
        var analyzed = Analyzed.Source(sources).Model;
        var result = AnonymousPolicySources.Generate(sources);
        AnonymousPolicySources.AssertRefused(result, AnonymousPolicySources.Ambiguous);
        result.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldEqual(analyzed.Policies.Single(policy => policy.Name == "Anyone").Requirement);
        result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
        result.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
    }
}
