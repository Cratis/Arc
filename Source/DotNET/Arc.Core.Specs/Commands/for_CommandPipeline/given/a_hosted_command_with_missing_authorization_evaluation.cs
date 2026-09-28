// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_hosted_command_with_missing_authorization_evaluation : a_command_pipeline
{
    protected ICommandHandler _handler;

    void Establish()
    {
        _handler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(new ProtectedCommand(), out anyHandler).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new PolicyDeclaration()])));
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns((object?)null);
    }

    public record ProtectedCommand;

    class PolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(ProtectedCommand) ? [AuthorizationRequirement.FromAttribute(null, "Protected", null)] : [];
    }
}
