// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_authorization_rechecking_swallows_the_snapshot_denial : given.a_command_pipeline_with_denied_snapshot_membership
{
    CommandResult _result;
    bool _argumentsResolved;
    bool _recheckedAuthorization;
    bool _caughtDenial;

    void Establish()
    {
        _commandHandlerArgumentResolver
            .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(_ =>
            {
                _argumentsResolved = true;
                return new ValueTask<CommandHandlerArgumentResolution>(new CommandHandlerArgumentResolution([], CommandResult.Success(_correlationId)));
            });
        var evaluator = new RecheckingEvaluator(() =>
        {
            // Only the final declaration lookup runs after argument resolution and its membership guard.
            if (_argumentsResolved)
            {
                _recheckedAuthorization = true;
                try
                {
                    _ = OperationSnapshot.Current!.Tenant;
                }
                catch (TenantMembershipDenied)
                {
                    _caughtDenial = true;
                }
            }
        });
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([evaluator])));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_recheck_authorization_after_argument_resolution() => _recheckedAuthorization.ShouldBeTrue();
    [Fact] void should_catch_the_snapshot_denial() => _caughtDenial.ShouldBeTrue();
    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_execute_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    /// <summary>
    /// A hand-written evaluator: substitutes cannot intercept the interface's default-implemented members.
    /// </summary>
    /// <param name="onTypeLookup">Callback invoked on every type requirement lookup.</param>
    class RecheckingEvaluator(Action onTypeLookup) : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type)
        {
            onTypeLookup();
            return [];
        }

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => [];
    }
}
