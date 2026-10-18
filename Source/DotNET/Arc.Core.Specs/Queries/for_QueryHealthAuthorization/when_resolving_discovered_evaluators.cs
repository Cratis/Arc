// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Queries.for_QueryHealthAuthorization;

[Collection("UsesCurrentDirectory")]
public class when_resolving_discovered_evaluators
{
    [Theory]
    [InlineData(null)]
    [InlineData("Development")]
    [InlineData("Production")]
    public void should_resolve_all_evaluators_and_preserve_unrelated_authorization(string? environmentName)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.Configure<ArcOptions>(_ => { });
        if (environmentName is not null)
        {
            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            services.AddSingleton(environment);
        }
        services.AddCratisArcCore();
        using var provider = services.BuildServiceProvider();
        (provider.GetService<IHostEnvironment>()?.EnvironmentName).ShouldEqual(environmentName);
        var anonymous = provider.GetRequiredService<IInstancesOf<IAnonymousEvaluator>>().ToArray();
        var attributes = provider.GetRequiredService<IInstancesOf<IAuthorizationAttributeEvaluator>>().ToArray();
        var expectedAnonymous = environmentName is null ? RuntimeEnvironment.IsDevelopment : environmentName == "Development";
        anonymous.OfType<QueryHealthAuthorization>().Single().IsAnonymousAllowed(typeof(QueryHealth)).ShouldEqual(expectedAnonymous);
        attributes.OfType<QueryHealthAuthorization>().Single().GetAuthorizationInfo(typeof(QueryHealth)).HasValue.ShouldEqual(!expectedAnonymous);
        provider.GetRequiredService<IAuthorizationEvaluator>().IsAuthorized(typeof(string)).ShouldBeTrue();

        var health = Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        ((IAuthorizationQueryTarget)health).AuthorizationMethod.Returns(typeof(QueryHealth).GetMethod(nameof(QueryHealth.ObserveHealth)));
        QueryHealthEndpointAccess.ForQuery(health, provider).ShouldEqual(expectedAnonymous);
    }
}
