// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_a_changed_verdict_principal : given.a_model_bound_query_performer
{
    Exception? _error;
    bool _invoked;

    void Establish()
    {
        EstablishPerformer<ProtectedQuery>(nameof(ProtectedQuery.Load));
        var initial = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "original")], "test"));
        var current = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "replacement")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(current);
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]));
        var services = new ServiceCollection().AddSingleton(declarations).AddSingleton(accessor).AddSingleton<ICurrentPrincipalAccessor>(accessor).BuildServiceProvider();
        _context = _context with { ServiceProvider = services };
        _context.AuthorizedExecution = new AuthorizedExecution(
            _performer.AuthorizationMethod,
            AuthorizationPrincipalIdentity.Capture(initial),
            declarations.For(_performer.AuthorizationMethod),
            false);
    }

    async Task Because()
    {
        ProtectedQuery.OnInvoke = () => _invoked = true;
        try
        {
            _error = await Catch.Exception(PerformQuery);
        }
        finally
        {
            ProtectedQuery.OnInvoke = null;
        }
    }

    [Fact] void should_throw_when_the_verdict_principal_is_no_longer_current() => _error.ShouldBeOfExactType<AuthorizationIdentityChanged>();
    [Fact] void should_not_invoke_the_method() => _invoked.ShouldBeFalse();

    public record ProtectedQuery
    {
        public static Action? OnInvoke { get; set; }
        public static ProtectedQuery Load()
        {
            OnInvoke?.Invoke();
            return new();
        }
    }
}
