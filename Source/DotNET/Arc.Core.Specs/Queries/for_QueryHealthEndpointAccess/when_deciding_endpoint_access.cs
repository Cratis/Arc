// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Queries.for_QueryHealthEndpointAccess;

public class when_deciding_endpoint_access
{
    [Theory]
    [InlineData("Development", null, null, true)]
    [InlineData("Production", null, null, false)]
    [InlineData("Development", true, null, false)]
    [InlineData("Production", false, null, true)]
    [InlineData("Development", false, "Admin", false)]
    public void should_exempt_only_health_from_host_fallback(string environmentName, bool? requireAuthentication, string? roles, bool anonymous)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var services = new ServiceCollection();
        services.AddSingleton(environment);
        services.Configure<ArcOptions>(options =>
        {
            options.QueryHealth.AuthenticationOverride = requireAuthentication;
            options.QueryHealth.Roles = roles;
        });
        using var provider = services.BuildServiceProvider();
        var health = Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        ((IAuthorizationQueryTarget)health).AuthorizationMethod.Returns(typeof(QueryHealth).GetMethod(nameof(QueryHealth.ObserveHealth)));
        QueryHealthEndpointAccess.ForQuery(health, provider).ShouldEqual(anonymous);
        QueryEndpointAccess.AllowsAnonymous(health, provider).ShouldEqual(anonymous);

        var other = Substitute.For<IQueryPerformer>();
        other.Type.Returns(typeof(OtherQuery));
        other.Name.Returns(new QueryName(nameof(OtherQuery.ObserveHealth)));
        QueryHealthEndpointAccess.ForQuery(other, provider).ShouldBeFalse();
        QueryEndpointAccess.AllowsAnonymous(other, provider).ShouldBeFalse();

        var opaqueHealth = Substitute.For<IQueryPerformer>();
        opaqueHealth.Type.Returns(typeof(QueryHealth));
        opaqueHealth.Name.Returns(new QueryName(nameof(QueryHealth.ObserveHealth)));
        QueryHealthEndpointAccess.ForQuery(opaqueHealth, provider).ShouldEqual(anonymous);
    }

    [Fact]
    public void should_deny_an_exception_without_configured_options()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var health = Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        ((IAuthorizationQueryTarget)health).AuthorizationMethod.Returns(typeof(QueryHealth).GetMethod(nameof(QueryHealth.ObserveHealth)));
        QueryHealthEndpointAccess.ForQuery(health, provider).ShouldBeFalse();
        QueryEndpointAccess.AllowsAnonymous(health, provider).ShouldBeFalse();
    }

    [Fact]
    public void should_not_let_a_custom_anonymous_evaluator_bypass_host_fallback()
    {
        var evaluator = Substitute.For<IAnonymousEvaluator>();
        evaluator.IsAnonymousAllowed(Arg.Any<Type>()).Returns(true);
        evaluator.IsAnonymousAllowed(Arg.Any<MethodInfo>()).Returns(true);
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new[] { evaluator }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => Enumerable.Empty<IAuthorizationAttributeEvaluator>().GetEnumerator());
        var services = new ServiceCollection();
        services.AddSingleton(new AuthorizationDeclarations(anonymous, attributes));
        services.AddSingleton(Substitute.For<IAuthorizationPolicyRuntime>());
        services.Configure<ArcOptions>(options => options.QueryHealth.RequireAuthentication = false);
        using var provider = services.BuildServiceProvider();
        var other = Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        ((IAuthorizationQueryTarget)other).AuthorizationMethod.Returns(typeof(OtherQuery).GetMethod(nameof(OtherQuery.ObserveHealth)));
        QueryEndpointAccess.AllowsAnonymous(other, provider).ShouldBeFalse();
    }

    public static class OtherQuery
    {
        public static int ObserveHealth() => 0;
    }
}
