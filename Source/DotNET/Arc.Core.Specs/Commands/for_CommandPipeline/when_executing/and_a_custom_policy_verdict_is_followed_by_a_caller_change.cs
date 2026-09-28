// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_custom_policy_verdict_is_followed_by_a_caller_change : given.a_command_pipeline
{
    ClaimsPrincipal _principal;
    ClaimsPrincipal _replacement;
    ICommandHandler _handler;
    CommandResult _guestResult;
    CommandResult _authenticatedResult;
    bool _guestVerdictBeforeSwitch;
    bool _authenticatedVerdictBeforeSwitch;

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
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(_ => _principal);
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var legacy = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var evaluation = new AuthorizationEvaluation(
            declarations,
            legacy,
            accessor,
            new ArcAuthorizationPolicyRuntime(
                [new AuthorizationPolicyRegistration("Custom", typeof(AllowingPolicy)) { EvaluatesAnonymous = true }]));
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(evaluation);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(AllowingPolicy)).Returns(new AllowingPolicy());

        var filter = new AuthorizationFilter(legacy);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call => filter.OnExecution(call.Arg<CommandContext>()));
        _commandHandlerArgumentResolver.Resolve(
            Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call =>
            {
                var context = call.Arg<CommandContext>();
                var verdictPresentWithoutPreparation = context.PreparedAuthorization is null && context.AuthorizedExecution is not null;
                if (_principal.Identity?.IsAuthenticated == true)
                {
                    _authenticatedVerdictBeforeSwitch = verdictPresentWithoutPreparation;
                }
                else
                {
                    _guestVerdictBeforeSwitch = verdictPresentWithoutPreparation;
                }

                _principal = _replacement;
                return new ValueTask<CommandHandlerArgumentResolution>(new CommandHandlerArgumentResolution([], CommandResult.Success(_correlationId)));
            });
    }

    async Task Because()
    {
        _principal = Principal("guest", authenticated: false);
        _replacement = Principal("signed-in", authenticated: true);
        _guestResult = await _commandPipeline.Execute(new CustomPolicyCommand(), _serviceProvider);

        _principal = Principal("caller-a", authenticated: true);
        _replacement = Principal("caller-b", authenticated: true);
        _authenticatedResult = await _commandPipeline.Execute(new CustomPolicyCommand(), _serviceProvider);
    }

    [Fact] void should_evaluate_the_custom_policy_without_preparing_the_guest_command() => _guestVerdictBeforeSwitch.ShouldBeTrue();
    [Fact] void should_evaluate_the_custom_policy_without_preparing_the_authenticated_command() => _authenticatedVerdictBeforeSwitch.ShouldBeTrue();
    [Fact] void should_deny_a_guest_verdict_after_the_caller_authenticates() => _guestResult.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_an_authenticated_verdict_after_the_caller_changes() => _authenticatedResult.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_handler_for_either_caller() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    static ClaimsPrincipal Principal(string name, bool authenticated) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], authenticated ? "test" : null));

    public record CustomPolicyCommand;

    class CustomPolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => type == typeof(CustomPolicyCommand) ? (true, null) : null;

        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(CustomPolicyCommand) ? [AuthorizationRequirement.FromAttribute(null, "Custom", null)] : [];
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
