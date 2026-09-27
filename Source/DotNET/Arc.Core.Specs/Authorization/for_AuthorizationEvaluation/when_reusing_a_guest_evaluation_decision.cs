// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_reusing_a_guest_evaluation_decision : Specification
{
    Exception? _failure;
    int _policyCalls;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        var runtime = new ChangingRuntime();
        var evaluation = new AuthorizationEvaluation(
            new AuthorizationDeclarations(anonymous, attributes),
            Substitute.For<IAuthorizationEvaluator>(),
            accessor,
            runtime);
        await using var services = new ServiceCollection().BuildServiceProvider();
        var prepared = await evaluation.Prepare(typeof(GuestCommand), services, CancellationToken.None);
        var context = new CommandContext(CorrelationId.New(), typeof(GuestCommand), new GuestCommand(), [], CommandContextValues.Empty)
        {
            PreparedAuthorization = prepared
        };
        runtime.EvaluatesAnonymous = false;
        _failure = await Catch.Exception(() => evaluation.IsAuthorized(typeof(GuestCommand), context, services, CancellationToken.None));
        _policyCalls = runtime.PolicyCalls;
    }

    [Fact] void should_reject_an_opt_in_removed_after_preparation() => _failure.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_not_run_the_old_guest_policy() => _policyCalls.ShouldEqual(0);

    [Authorize(Policy = "Guest")]
    public record GuestCommand;

    class ChangingRuntime : IAuthorizationPolicyRuntime
    {
        public bool EvaluatesAnonymous { get; set; } = true;
        public int PolicyCalls { get; private set; }

        public Task Validate(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IAuthorizationPolicyResolution> Resolve(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) =>
            Task.FromResult<IAuthorizationPolicyResolution>(new Resolution(this, EvaluatesAnonymous));

        class Resolution(ChangingRuntime owner, bool evaluatesAnonymous) : IAuthorizationPolicyResolution, IAnonymousPolicyResolution
        {
            public bool EvaluatesAnonymous => evaluatesAnonymous;

            public Task<ClaimsPrincipal?> SelectPrincipal(ClaimsPrincipal? principal, IServiceProvider services, CancellationToken cancellationToken) => Task.FromResult(principal);

            public Task<bool> IsAuthorized(AuthorizationPolicyContext context, IServiceProvider services, CancellationToken cancellationToken)
            {
                owner.PolicyCalls++;
                return Task.FromResult(true);
            }
        }
    }
}
