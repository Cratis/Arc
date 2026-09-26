// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations;

public class when_resolving_baseline_requirements : Specification
{
    AuthorizationDeclarations _declarations;
    AuthorizationEvaluator _evaluator;
    ChangingFallback _changing;

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

    [Fact] void should_apply_the_baseline_to_a_type_without_an_explicit_declaration() =>
        _declarations.For(typeof(Plain)).Requirements.Single().AnyOfRoles.Single().ShouldEqual("Baseline");

    [Fact] void should_require_all_type_and_method_fallback_requirements() =>
        _declarations.For(typeof(Plain).GetMethod(nameof(Plain.Run))!).Requirements.Count.ShouldEqual(2);

    [Fact] void should_not_apply_a_fallback_to_an_anonymous_method() =>
        _declarations.For(typeof(Plain).GetMethod(nameof(Plain.Public))!).AllowsAnonymous.ShouldBeTrue();

    [Fact] void should_not_apply_a_fallback_to_an_explicit_type() =>
        _declarations.For(typeof(TypeRestricted).GetMethod(nameof(TypeRestricted.Run))!).Requirements.Single().AnyOfRoles.Single().ShouldEqual("Type");

    [Fact] void should_prefer_an_explicit_method_over_its_type_and_fallbacks() =>
        _declarations.For(typeof(TypeRestricted).GetMethod(nameof(TypeRestricted.Restricted))!).Requirements.Single().AnyOfRoles.Single().ShouldEqual("Method");

    [Fact] void should_allow_an_anonymous_method_despite_its_explicit_type_and_fallbacks() =>
        _declarations.For(typeof(TypeRestricted).GetMethod(nameof(TypeRestricted.Public))!).AllowsAnonymous.ShouldBeTrue();

    [Fact] void should_not_consult_fallbacks_for_explicit_declarations()
    {
        _declarations.For(typeof(Plain).GetMethod(nameof(Plain.Public))!);
        _declarations.For(typeof(TypeRestricted).GetMethod(nameof(TypeRestricted.Run))!);
        _declarations.For(typeof(TypeRestricted).GetMethod(nameof(TypeRestricted.Restricted))!);
        _changing.ReadCount.ShouldEqual(0);
    }

    [Fact] void should_enforce_both_fallbacks_when_evaluating_the_principal() =>
        _evaluator.IsAuthorized(typeof(Plain).GetMethod(nameof(Plain.Run))!).ShouldBeFalse();

    [Fact] void should_not_reuse_a_previous_fallback_result()
    {
        var first = _declarations.For(typeof(Plain)).Requirements.Single().AnyOfRoles.Single();
        _changing.Role = "Changed";
        var second = _declarations.For(typeof(Plain)).Requirements.Single().AnyOfRoles.Single();
        first.ShouldEqual("Baseline");
        second.ShouldEqual("Changed");
    }

    [Fact] void should_still_reject_an_explicit_evaluator_conflicting_with_anonymous_access()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new ExplicitRequirement() }.AsEnumerable().GetEnumerator());
        var exception = Catch.Exception(() => new AuthorizationDeclarations(anonymous, attributes).For(typeof(Plain).GetMethod(nameof(Plain.Public))!));
        exception.ShouldBeOfExactType<AmbiguousAuthorizationLevel>();
        exception.Message.ShouldContain(nameof(AnonymousEvaluator));
        exception.Message.ShouldContain(nameof(ExplicitRequirement));
        exception.Message.ShouldNotContain("has both [Authorize]");
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

    public class ExplicitRequirement : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;

        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) =>
            method.DeclaringType == typeof(Plain) && method.Name == nameof(Plain.Public) ? (true, null) : null;
    }
}
