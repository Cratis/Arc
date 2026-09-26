// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_no_operation_receipt_exists : Specification
{
    readonly DateTimeOffset _now = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    readonly List<DateTimeOffset> _observed = [];
    DateTimeOffset? _after;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_now);
        var services = new ServiceCollection().AddSingleton<TimeProvider>(clock).AddSingleton(new CapturePolicy(_observed)).BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(
            new AuthorizationDeclarations(anonymous, attributes),
            Substitute.For<IAuthorizationEvaluator>(),
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Capture", typeof(CapturePolicy))]));
        var command = new ProtectedCommand();
        var handBuilt = new CommandContext(CorrelationId.New(), typeof(ProtectedCommand), command, [], new());
        await evaluation.IsAuthorized(typeof(ProtectedCommand), handBuilt, services, CancellationToken.None);
        await evaluation.IsAuthorized(typeof(ProtectedCommand), command, services, CancellationToken.None);
        _after = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_use_the_configured_clock_for_hand_built_and_other_resources() => _observed.ShouldEqual(new[] { _now, _now });
    [Fact] void should_not_create_a_leaked_operation_scope() => _after.ShouldBeNull();

    [Authorize(Policy = "Capture")]
    public record ProtectedCommand;

    public class CapturePolicy(List<DateTimeOffset> observed) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            observed.Add(context.ReceivedAt);
            return ValueTask.FromResult(true);
        }
    }
}
