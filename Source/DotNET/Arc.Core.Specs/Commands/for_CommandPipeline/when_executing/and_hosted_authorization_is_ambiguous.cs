// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_hosted_authorization_is_ambiguous : given.a_command_pipeline
{
    CommandResult _execute;
    CommandResult _validate;

    void Establish()
    {
        var anonymous = Substitute.For<IAnonymousEvaluator>();
        anonymous.IsAnonymousAllowed(typeof(ConflictingCommand)).Returns(true);
        var restricted = Substitute.For<IAnonymousEvaluator>();
        restricted.IsAnonymousAllowed(typeof(ConflictingCommand)).Returns(false);
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([anonymous, restricted]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])));
    }

    async Task Because()
    {
        _execute = await _commandPipeline.ExecuteHosted(new ConflictingCommand(), _serviceProvider, null, CancellationToken.None);
        _validate = await _commandPipeline.ValidateHosted(new ConflictingCommand(), _serviceProvider, null, CancellationToken.None);
    }

    [Fact] void should_report_unauthorized_for_execute_and_validate() => new[] { _execute, _validate }.All(result => !result.IsAuthorized && !result.HasExceptions).ShouldBeTrue();
    [Fact] void should_not_run_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());

    public record ConflictingCommand;
}
