// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_policy_is_evaluated_twice_for_one_operation : Specification
{
    readonly DateTimeOffset _receipt = new(2026, 4, 5, 6, 7, 8, TimeSpan.Zero);
    readonly List<DateTimeOffset> _policyReceipts = [];
    readonly List<DateTimeOffset> _resourceReceipts = [];
    bool _firstAllowed;
    bool _secondAllowed;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test"));
        var principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        principalAccessor.Current.Returns(principal);
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(ProtectedCommand)).Returns(true);
        var runtime = new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Capture", typeof(CapturePolicy))]);
        var services = new ServiceCollection().AddSingleton(new CapturePolicy(_policyReceipts, _resourceReceipts)).BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(new AuthorizationDeclarations(anonymous, attributes), evaluator, principalAccessor, runtime);
        var command = new ProtectedCommand();
        var resource = new CommandContext(CorrelationId.New(), typeof(ProtectedCommand), command, [], new()) { ReceivedAt = _receipt };
        _firstAllowed = await evaluation.IsAuthorized(typeof(ProtectedCommand), resource, services, CancellationToken.None);
        _secondAllowed = await evaluation.IsAuthorized(typeof(ProtectedCommand), resource, services, CancellationToken.None);
    }

    [Fact] void should_allow_both_checks() => (_firstAllowed && _secondAllowed).ShouldBeTrue();
    [Fact] void should_pass_the_same_receipt_on_every_policy_evaluation() => _policyReceipts.ShouldEqual(new[] { _receipt, _receipt });
    [Fact] void should_preserve_the_resource_receipt() => _resourceReceipts.ShouldEqual(new[] { _receipt, _receipt });

    [Authorize(Policy = "Capture")]
    public record ProtectedCommand;

    public class CapturePolicy(List<DateTimeOffset> receipts, List<DateTimeOffset> resources) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            receipts.Add(context.ReceivedAt);
            resources.Add(((CommandContext)context.Resource).ReceivedAt);
            return ValueTask.FromResult(true);
        }
    }
}
