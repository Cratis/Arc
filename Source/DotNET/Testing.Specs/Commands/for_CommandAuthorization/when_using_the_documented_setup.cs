// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_using_the_documented_setup : Specification
{
    ServiceProvider _root;
    IServiceScope _scope;
    CommandAuthorizationResult _result;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        services.AddSingleton<CurrentPrincipalAccessor>();
        services.AddSingleton<ICurrentPrincipalAccessor>(provider => provider.GetRequiredService<CurrentPrincipalAccessor>());
        services.AddSingleton<ICurrentPrincipalOverride>(provider => provider.GetRequiredService<CurrentPrincipalAccessor>());
        services.AddSingleton<IInstancesOf<IAnonymousEvaluator>>(new KnownInstancesOf<IAnonymousEvaluator>([new AnonymousEvaluator()]));
        services.AddSingleton<IInstancesOf<IAuthorizationAttributeEvaluator>>(new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]));
        services.AddSingleton<AuthorizationDeclarations>();
        services.AddSingleton<IAuthorizationEvaluator, AuthorizationEvaluator>();
        services.AddScoped<IAuthorizationPolicyRuntime, ArcAuthorizationPolicyRuntime>();
        services.AddScoped<AuthorizationEvaluation>();
        _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _root.CreateScope();
    }

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Writer")], "spec"));
        _result = await CommandAuthorization.For<given.RoleCommand>(_scope.ServiceProvider, principal);
    }

    [Fact] void should_authorize_the_writer() => _result.ShouldBeAuthorized();

    void Destroy()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}
