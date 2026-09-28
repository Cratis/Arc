// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_a_prepared_policy_but_no_verdict : given.a_model_bound_query_performer
{
    Exception? _error;
    bool _invoked;

    void Establish()
    {
        EstablishPerformer<ProtectedQuery>(nameof(ProtectedQuery.Load));
        var declaration = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]))
            .For(_performer.AuthorizationMethod);
        _context.PreparedAuthorization = new PreparedAuthorization(
            _performer.AuthorizationMethod,
            declaration,
            null,
            null,
            Substitute.For<IAuthorizationPolicyResolution>());
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

    [Fact] void should_throw_without_a_policy_verdict() => _error.ShouldBeOfExactType<AuthorizationIdentityChanged>();
    [Fact] void should_not_invoke_the_method() => _invoked.ShouldBeFalse();

    [Authorize(Policy = "Allow")]
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
