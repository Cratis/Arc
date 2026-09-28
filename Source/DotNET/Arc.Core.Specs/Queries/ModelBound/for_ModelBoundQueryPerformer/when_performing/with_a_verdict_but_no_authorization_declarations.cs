// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_a_verdict_but_no_authorization_declarations : given.a_model_bound_query_performer
{
    Exception? _error;
    bool _invoked;

    void Establish()
    {
        EstablishPerformer<ProtectedQuery>(nameof(ProtectedQuery.Load));
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([]));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        _context = _context with { ServiceProvider = new ServiceCollection().AddSingleton(accessor).BuildServiceProvider() };
        _context.AuthorizedExecution = new AuthorizedExecution(
            _performer.AuthorizationMethod,
            AuthorizationPrincipalIdentity.Capture(null),
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

    [Fact] void should_fail_closed_when_declarations_are_missing() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_not_invoke_the_query() => _invoked.ShouldBeFalse();

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
