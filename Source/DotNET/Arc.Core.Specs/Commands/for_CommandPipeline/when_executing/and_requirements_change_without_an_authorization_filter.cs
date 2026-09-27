// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_requirements_change_without_an_authorization_filter : given.a_command_pipeline
{
    ICommandHandler _handler;
    ChangingRequirements _evaluator;
    CommandResult _result;

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

        _evaluator = new ChangingRequirements();
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([_evaluator]));
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);

        // This is an ordinary filter, not Arc's authorization filter. The first lookup sees no policy.
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(context =>
        {
            declarations.For(context.Arg<CommandContext>().Type).RequiresAsynchronousEvaluation.ShouldBeFalse();
            return Task.FromResult(CommandResult.Success(_correlationId));
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(new CustomPolicyCommand(), _serviceProvider);

    [Fact] void should_read_the_changed_requirements_before_invocation() => _evaluator.Reads.ShouldEqual(2);
    [Fact] void should_deny_the_new_policy() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    public record CustomPolicyCommand;

    class ChangingRequirements : IAuthorizationAttributeEvaluator
    {
        public int Reads { get; private set; }

        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(CustomPolicyCommand) && ++Reads > 1
                ? [AuthorizationRequirement.FromAttribute(null, "NewPolicy", null)] : [];
    }
}
