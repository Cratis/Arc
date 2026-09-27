// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizedExecution;

public class when_an_evaluator_mutates_a_shared_requirement : Specification
{
    bool _current;

    void Because()
    {
        var evaluator = new SharedRequirement();
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([evaluator]));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        var verdict = new AuthorizedExecution(
            typeof(ProtectedCommand),
            AuthorizationPrincipalIdentity.Capture(principal),
            declarations.For(typeof(ProtectedCommand)),
            false);
        evaluator.Roles[0] = "Admin";
        _current = verdict.IsCurrent(typeof(ProtectedCommand), accessor, declarations.For(typeof(ProtectedCommand)));
    }

    [Fact] void should_not_reuse_a_verdict_for_mutated_roles() => _current.ShouldBeFalse();

    public record ProtectedCommand;

    class SharedRequirement : IAuthorizationAttributeEvaluator
    {
        public string[] Roles { get; } = ["Member"];
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) => type == typeof(ProtectedCommand)
            ? [new AuthorizationRequirement(Roles) { Policy = "Allow" }] : [];
    }
}
