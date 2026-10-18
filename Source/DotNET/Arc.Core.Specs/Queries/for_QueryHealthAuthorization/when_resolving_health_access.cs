// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryHealthAuthorization;

public class when_resolving_health_access
{
    static readonly Dictionary<AccessCase, (string EnvironmentName, bool? RequireAuthentication, string? Roles, string? CallerRole, bool Allowed)> _settings = new()
    {
        [AccessCase.Development] = ("Development", null, null, null, true),
        [AccessCase.Production] = ("Production", null, null, null, false),
        [AccessCase.Staging] = ("Staging", null, null, null, false),
        [AccessCase.ExplicitAuthentication] = ("Development", true, null, null, false),
        [AccessCase.ExplicitAnonymous] = ("Production", false, null, null, true),
        [AccessCase.Authenticated] = ("Production", true, null, "Reader", true),
        [AccessCase.MatchingRole] = ("Production", true, "Admin, Reader", "Reader", true),
        [AccessCase.MissingRole] = ("Production", true, "Admin, Reader", "Other", false),
        [AccessCase.RolesOverrideAnonymous] = ("Development", false, "Admin", null, false)
    };

    public enum AccessCase
    {
        Development = 0,
        Production = 1,
        Staging = 2,
        ExplicitAuthentication = 3,
        ExplicitAnonymous = 4,
        Authenticated = 5,
        MatchingRole = 6,
        MissingRole = 7,
        RolesOverrideAnonymous = 8
    }

    [Theory]
    [InlineData(AccessCase.Development)]
    [InlineData(AccessCase.Production)]
    [InlineData(AccessCase.Staging)]
    [InlineData(AccessCase.ExplicitAuthentication)]
    [InlineData(AccessCase.ExplicitAnonymous)]
    [InlineData(AccessCase.Authenticated)]
    [InlineData(AccessCase.MatchingRole)]
    [InlineData(AccessCase.MissingRole)]
    [InlineData(AccessCase.RolesOverrideAnonymous)]
    public void should_apply_the_same_declaration_to_the_type_and_observer(AccessCase accessCase)
    {
        var (environmentName, requireAuthentication, roles, callerRole, allowed) = _settings[accessCase];
        var options = new ArcOptions();
        options.QueryHealth.AuthenticationOverride = requireAuthentication;
        options.QueryHealth.Roles = roles;
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var evaluator = new QueryHealthAuthorization(Options.Create(options), environment);
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator(), evaluator }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator(), evaluator }.AsEnumerable().GetEnumerator());
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var principal = new ClaimsPrincipal(callerRole is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity([new Claim(ClaimTypes.Role, callerRole)], "test"));
        var typeDeclaration = declarations.For(typeof(QueryHealth));
        var methodDeclaration = declarations.For(typeof(QueryHealth).GetMethod(nameof(QueryHealth.ObserveHealth))!);

        typeDeclaration.IsExplicit.ShouldBeTrue();
        methodDeclaration.IsExplicit.ShouldBeTrue();
        AuthorizationEvaluator.Check(typeDeclaration, principal).ShouldEqual(allowed);
        AuthorizationEvaluator.Check(methodDeclaration, principal).ShouldEqual(allowed);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void should_not_declare_access_for_other_members(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var evaluator = new QueryHealthAuthorization(Options.Create(new ArcOptions()), environment);
        evaluator.IsAnonymousAllowed(typeof(OtherQuery)).ShouldBeNull();
        evaluator.IsAnonymousAllowed(typeof(OtherQuery).GetMethod(nameof(OtherQuery.ObserveHealth))!).ShouldBeNull();
        evaluator.GetAuthorizationInfo(typeof(OtherQuery)).ShouldBeNull();
        evaluator.GetAuthorizationInfo(typeof(OtherQuery).GetMethod(nameof(OtherQuery.ObserveHealth))!).ShouldBeNull();
        evaluator.IsAnonymousAllowed(typeof(QueryHealth).GetMethod(nameof(QueryHealth.ToString), Type.EmptyTypes)!).ShouldBeNull();
    }

    public static class OtherQuery
    {
        public static int ObserveHealth() => 0;
    }
}
