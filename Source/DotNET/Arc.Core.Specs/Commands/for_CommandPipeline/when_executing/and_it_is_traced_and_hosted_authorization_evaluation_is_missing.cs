// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// A hosted command whose authorization cannot be prepared is turned away before the pipeline proper runs. The span
/// still says why.
/// </summary>
public class and_it_is_traced_and_hosted_authorization_evaluation_is_missing : given.a_traced_command_pipeline
{
    public record ProtectedCommand;

    ProtectedCommand _protected;

    void Establish()
    {
        _protected = new();
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new PolicyDeclaration()])));
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns((object?)null);
    }

    async Task Because() => _result = await _commandPipeline.ExecuteHosted(_protected, _serviceProvider, null, CancellationToken.None);

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_add_the_authorization_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Authorization);
    [Fact] void should_record_the_exception_type() => CommandSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).Tags.Single().Value.ShouldEqual(typeof(InvalidAuthorizationConfiguration).FullName);

    class PolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(ProtectedCommand) ? [AuthorizationRequirement.FromAttribute(null, "Protected", null)] : [];
    }
}
