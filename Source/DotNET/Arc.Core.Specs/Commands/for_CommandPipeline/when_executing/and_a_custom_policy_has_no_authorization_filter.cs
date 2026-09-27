// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_custom_policy_has_no_authorization_filter : given.a_command_pipeline
{
    ICommandHandler _handler;
    CommandResult _withoutFilter;
    CommandResult _withReplacementFilter;
    bool _replacementFilterCalled;

    void Establish()
    {
        var command = new CustomPolicyCommand();
        _handler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(command, out anyHandler).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });

        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => Array.Empty<IAnonymousEvaluator>().AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new CustomPolicyDeclaration() }.AsEnumerable().GetEnumerator());
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(new AuthorizationEvaluation(
            declarations,
            Substitute.For<IAuthorizationEvaluator>(),
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Custom", typeof(AllowingPolicy))])));
    }

    async Task Because()
    {
        _withoutFilter = await _commandPipeline.Execute(new CustomPolicyCommand(), _serviceProvider);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            _replacementFilterCalled = true;
            return CommandResult.Success(call.Arg<CommandContext>().CorrelationId);
        });
        _withReplacementFilter = await _commandPipeline.Execute(new CustomPolicyCommand(), _serviceProvider);
    }

    [Fact] void should_deny_when_authorization_filter_is_omitted() => _withoutFilter.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_when_authorization_filter_is_replaced() => _withReplacementFilter.IsAuthorized.ShouldBeFalse();
    [Fact] void should_run_the_replacement_filter() => _replacementFilterCalled.ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    public record CustomPolicyCommand;

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    class CustomPolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(CustomPolicyCommand) ? [AuthorizationRequirement.FromAttribute(null, "Custom", null)] : [];
    }
}
