// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

[Collection("UsesCurrentDirectory")]
public class when_aspnet_host_uses_fallback_requirements : Specification
{
    AuthorizationDeclaration _baseline;
    AuthorizationDeclaration _anonymous;
    AuthorizationDeclaration _explicitType;
    AuthorizationDeclaration _explicitMethod;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        await using var app = builder.Build();
        await using var scope = app.Services.CreateAsyncScope();
        var declarations = scope.ServiceProvider.GetRequiredService<AuthorizationDeclarations>();
        _baseline = declarations.For(typeof(BaselineController).GetMethod(nameof(BaselineController.Private))!);
        _anonymous = declarations.For(typeof(BaselineController).GetMethod(nameof(BaselineController.Public))!);
        _explicitType = declarations.For(typeof(RestrictedController).GetMethod(nameof(RestrictedController.Private))!);
        _explicitMethod = declarations.For(typeof(RestrictedController).GetMethod(nameof(RestrictedController.Public))!);
    }

    [Fact] void should_require_a_baseline_for_an_unannotated_controller_query() => _baseline.Requirements.Single().AnyOfRoles.Single().ShouldEqual("Member");
    [Fact] void should_let_aspnet_anonymous_override_the_baseline() => _anonymous.AllowsAnonymous.ShouldBeTrue();
    [Fact] void should_let_aspnet_type_authorize_replace_the_baseline() => _explicitType.Requirements.Single().AnyOfRoles.Single().ShouldEqual("Manager");
    [Fact] void should_let_arc_method_authorize_replace_the_type_and_baseline() => _explicitMethod.Requirements.Single().AnyOfRoles.Single().ShouldEqual("Admin");

    public class BaselineController : ControllerBase
    {
        public string Private() => "private";

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public string Public() => "public";
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
    public class RestrictedController : ControllerBase
    {
        public string Private() => "private";

        [Authorize(Roles = "Admin")]
        public string Public() => "public";
    }

    public class MemberBaseline : IFallbackAuthorizationEvaluator
    {
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(BaselineController) || type == typeof(RestrictedController) ? [AuthorizationRequirement.FromRoles("Member")] : [];

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => [];
    }
}
