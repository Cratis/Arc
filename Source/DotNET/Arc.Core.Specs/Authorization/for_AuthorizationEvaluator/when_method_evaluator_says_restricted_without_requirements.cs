// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_method_evaluator_says_restricted_without_requirements : Specification
{
    bool _authorized;

    void Because()
    {
        var anonymous = Substitute.For<IAnonymousEvaluator>();
        anonymous.IsAnonymousAllowed(Arg.Any<MethodInfo>()).Returns(false);
        var anonymousEvaluators = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymousEvaluators.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { anonymous, new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributeEvaluators = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributeEvaluators.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        _authorized = new AuthorizationEvaluator(principal, anonymousEvaluators, attributeEvaluators)
            .IsAuthorized(typeof(ProtectedType).GetMethod(nameof(ProtectedType.All))!);
    }

    [Fact] void should_fall_back_to_the_restricted_type() => _authorized.ShouldBeFalse();

    [Authorize]
    public class ProtectedType
    {
        public void All()
        {
        }
    }
}
