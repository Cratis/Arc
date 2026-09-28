// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.Filters;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_requirements_change_after_the_policy_verdict : given.a_command_pipeline
{
    ICommandHandler _handler;
    ChangingRequirements _requirements;
    CommandResult _result;

    void Establish()
    {
        _handler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(new ProtectedCommand(), out anyHandler).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });

        _requirements = new ChangingRequirements();
        var anonymous = new KnownInstancesOf<IAnonymousEvaluator>([]);
        var attributes = new KnownInstancesOf<IAuthorizationAttributeEvaluator>([_requirements]);
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        var evaluator = new AuthorizationEvaluator(principal, anonymous, attributes);
        var evaluation = new AuthorizationEvaluation(
            declarations,
            evaluator,
            principal,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Allow", typeof(AllowingPolicy))]));
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(evaluation);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principal);
        _serviceProvider.GetService(typeof(AllowingPolicy)).Returns(new AllowingPolicy());
        var filter = new AuthorizationFilter(evaluator);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async call =>
        {
            var result = await filter.OnExecution(call.Arg<CommandContext>());
            _requirements.UseAdminRole = true;
            return result;
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(new ProtectedCommand(), _serviceProvider);

    [Fact] void should_deny_a_verdict_for_the_old_policy() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    public record ProtectedCommand;

    class ChangingRequirements : IAuthorizationAttributeEvaluator
    {
        public bool UseAdminRole { get; set; }
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) => type == typeof(ProtectedCommand)
            ? [AuthorizationRequirement.FromAttribute(UseAdminRole ? "Admin" : null, UseAdminRole ? null : "Allow", null)] : [];
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
