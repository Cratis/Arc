// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command.given;

public class an_unbound_command : for_CommandPipeline.given.a_command_pipeline
{
    protected ICurrentPrincipalAccessor _principal;
    protected IAuthorizationEvaluator _evaluator;
    protected DenyingPolicy _policy;
    protected CommandResult _bindingFailure;
    protected CommandResult _result;

    void Establish()
    {
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([new AnonymousEvaluator()]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]));
        _principal = Substitute.For<ICurrentPrincipalAccessor>();
        _principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Editor")], "test")));
        _evaluator = Substitute.For<IAuthorizationEvaluator>();
        _evaluator.IsAuthorized(Arg.Any<Type>()).Returns(true);
        _policy = new DenyingPolicy();
        _serviceProvider.GetService(typeof(DenyingPolicy)).Returns(_policy);
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);
        _serviceProvider.GetService(typeof(IAuthorizationEvaluator)).Returns(_evaluator);
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(new AuthorizationEvaluation(
            declarations,
            _evaluator,
            _principal,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Denying", typeof(DenyingPolicy))])));
        _bindingFailure = CommandBodyBindingFailure.From(_correlationId, new System.Text.Json.JsonException("private", "$.from", null, null));
    }

    [Authorize(Roles = "Editor")]
    public record RoleCommand;

    [Authorize(Policy = "Denying")]
    public record PolicyCommand;

    public class DenyingPolicy : IAuthorizationPolicy
    {
        public int Calls { get; private set; }
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Calls++;
            return ValueTask.FromResult(false);
        }
    }
}
