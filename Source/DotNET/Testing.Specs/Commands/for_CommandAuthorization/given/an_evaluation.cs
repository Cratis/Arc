// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

public class an_evaluation : Specification
{
    protected IServiceCollection Services;
    protected ClaimsPrincipal Principal;
    protected CommandAuthorizationResult Result;
    protected PolicyBehavior Behavior;
    protected CurrentPrincipalAccessor Accessor => Provider.GetRequiredService<CurrentPrincipalAccessor>();
    ServiceProvider _root;
    IServiceScope _scope;

    protected IServiceProvider Provider
    {
        get
        {
            _root ??= Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            _scope ??= _root.CreateScope();
            return _scope.ServiceProvider;
        }
    }

    void Establish()
    {
        Principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Writer")], "spec"));
        Behavior = new PolicyBehavior();
        Services = new ServiceCollection().AddLogging().AddOptions();
        Services.AddCratisArcCore();
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(RoleCommand), typeof(ResourceCommand), typeof(UnrestrictedCommand), typeof(NestedEvaluationCommand), typeof(ResourceValidator)]);
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ResourceValidator)]);
        Services.AddSingleton(types);
        Services.AddTransient<ResourceValidator>();
        Services.AddSingleton<IInstancesOf<IAnonymousEvaluator>>(new KnownInstancesOf<IAnonymousEvaluator>([new AnonymousEvaluator()]));
        Services.AddSingleton<IInstancesOf<IAuthorizationAttributeEvaluator>>(new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]));
        Services.AddSingleton<IInstancesOf<IFallbackAuthorizationEvaluator>>(new KnownInstancesOf<IFallbackAuthorizationEvaluator>([]));
        Services.AddSingleton<IInstancesOf<ICommandFilter>>(provider => new KnownInstancesOf<ICommandFilter>([new AuthorizationFilter(provider.GetRequiredService<IAuthorizationEvaluator>())]));
        Services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        Services.AddSingleton<IAuthorizationEvaluator, AuthorizationEvaluator>();
        Services.AddSingleton(Behavior);
        Services.AddArcAuthorizationPolicy<FixturePolicy>("fixture", evaluatesAnonymous: true);
    }

    void Destroy()
    {
        _scope?.Dispose();
        _root?.Dispose();
    }
}
