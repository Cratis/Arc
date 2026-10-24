// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class with_conflicting_anonymous_and_explicit_evaluators : given.a_baseline
{
    Exception _exception;

    void Establish()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new ExplicitRequirement() }.AsEnumerable().GetEnumerator());
        _declarations = new AuthorizationDeclarations(anonymous, attributes);
    }

    void Because() => _exception = Catch.Exception(() => _declarations.For(typeof(Plain).GetMethod(nameof(Plain.Public))!));

    [Fact]
    void should_still_reject_an_explicit_evaluator_conflicting_with_anonymous_access()
    {
        _exception.ShouldBeOfExactType<AmbiguousAuthorizationLevel>();
        _exception.Message.ShouldContain(nameof(AnonymousEvaluator));
        _exception.Message.ShouldContain(nameof(ExplicitRequirement));
        _exception.Message.ShouldNotContain("has both [Authorize]");
    }

    public class ExplicitRequirement : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;

        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) =>
            method.DeclaringType == typeof(Plain) && method.Name == nameof(Plain.Public) ? (true, null) : null;
    }
}
