// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_command_requires_approval_without_an_authorization_verdict : given.a_command_pipeline_and_an_approval_requiring_command
{
    void Establish()
    {
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new CustomPolicyDeclaration()]));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(new AuthorizationEvaluation(
            declarations,
            Substitute.For<IAuthorizationEvaluator>(),
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Custom", typeof(AllowingPolicy))])));
        _flow.Begin(Arg.Any<CommandContext>()).Returns(CommandResult.Success(_correlationId));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_refuse_before_approval_or_argument_resolution(bool registerFlow, bool replaceFilter)
    {
        if (registerFlow)
        {
            _serviceProvider.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(new[] { _flow });
        }
        var replacementCalled = false;
        if (replaceFilter)
        {
            _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
            {
                replacementCalled = true;
                return CommandResult.Success(call.Arg<CommandContext>().CorrelationId);
            });
        }

        var result = await _commandPipeline.Execute(_command, _serviceProvider, null, TestContext.Current.CancellationToken);

        result.AuthorizationConfigurationFailed.ShouldBeTrue();
        result.IsAuthorized.ShouldBeFalse();
        result.AuthorizationFailureReason.ShouldNotEqual(ApprovalGate.RefusalReason);
        replacementCalled.ShouldEqual(replaceFilter);
        await _flow.DidNotReceive().Begin(Arg.Any<CommandContext>());
        _commandHandlerArgumentResolver.ReceivedCalls().ShouldBeEmpty();
        await _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
        await _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), result);
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    class CustomPolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(ApprovalRequiringCommand) ? [AuthorizationRequirement.FromAttribute(null, "Custom", null)] : [];
    }
}
