// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryAuthorizationTarget;

public class when_a_custom_performer_has_method_policies : Specification
{
    MemberInfo _target;
    Exception? _ambiguous;

    void Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(CustomReadModel));
        performer.Name.Returns(new QueryName(nameof(CustomReadModel.All)));
        _target = QueryAuthorizationTarget.For(performer, declarations);
        performer.Name.Returns(new QueryName("OpaqueAlias"));
        _ambiguous = Catch.Exception(() => QueryAuthorizationTarget.For(performer, declarations));
    }

    [Fact] void should_enforce_the_named_method() => _target.ShouldEqual(typeof(CustomReadModel).GetMethod(nameof(CustomReadModel.All)));
    [Fact] void should_reject_an_opaque_alias_instead_of_silently_ignoring_the_policy() => _ambiguous.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();

    public static class CustomReadModel
    {
        [Authorize(Policy = "Protected")]
        public static void All()
        {
        }
    }
}
