// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements.given;

public class a_baseline : Specification
{
    protected AuthorizationDeclarations _declarations;
    protected AuthorizationEvaluator _evaluator;
    protected ChangingFallback _changing;

    void Establish()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        _changing = new ChangingFallback();
        var fallbacks = Substitute.For<IInstancesOf<IFallbackAuthorizationEvaluator>>();
        fallbacks.GetEnumerator().Returns(_ => new IFallbackAuthorizationEvaluator[] { _changing, new MethodFallback() }.AsEnumerable().GetEnumerator());
        _declarations = new AuthorizationDeclarations(anonymous, attributes, fallbacks);
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Baseline")], "test")));
        _evaluator = new AuthorizationEvaluator(accessor, anonymous, attributes, fallbacks);
    }

    public class Plain
    {
        public void Run() { }

        [AllowAnonymous]
        public void Public() { }
    }

    [Authorize(Roles = "Type")]
    public class TypeRestricted
    {
        public void Run() { }

        [AllowAnonymous]
        public void Public() { }

        [Authorize(Roles = "Method")]
        public void Restricted() { }
    }

    public class ChangingFallback : IFallbackAuthorizationEvaluator
    {
        public string Role { get; set; } = "Baseline";

        public int ReadCount { get; private set; }

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type)
        {
            ReadCount++;
            return type == typeof(Plain) || type == typeof(TypeRestricted) ? [AuthorizationRequirement.FromRoles(Role)] : [];
        }

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => [];
    }

    public class MethodFallback : IFallbackAuthorizationEvaluator
    {
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) => [];

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) =>
            method.DeclaringType == typeof(Plain) || method.DeclaringType == typeof(TypeRestricted)
                ? [AuthorizationRequirement.FromRoles("MethodBaseline")]
                : [];
    }
}
