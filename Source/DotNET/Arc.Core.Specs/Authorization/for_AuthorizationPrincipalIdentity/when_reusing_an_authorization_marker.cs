// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity;

public class when_reusing_an_authorization_marker : Specification
{
    [Fact]
    void should_not_reuse_the_marker_after_claims_change_on_the_same_principal()
    {
        var (evaluator, _, declaration, principal) = Create();
        using (AuthorizationEvaluator.AlreadyEvaluated(typeof(ProtectedCommand), principal, declaration))
        {
            principal.Identities.Single().AddClaim(new Claim("permission", "changed"));

            Catch.Exception(() => evaluator.IsAuthorized(typeof(ProtectedCommand))).ShouldBeOfExactType<AsynchronousAuthorizationRequired>();
        }
    }

    [Fact]
    void should_not_reuse_the_marker_after_actor_changes_on_the_same_principal()
    {
        var (evaluator, _, declaration, principal) = Create();
        principal.Identities.Single().Actor = new ClaimsIdentity([new Claim("actor", "original")], "test");
        using (AuthorizationEvaluator.AlreadyEvaluated(typeof(ProtectedCommand), principal, declaration))
        {
            principal.Identities.Single().Actor = new ClaimsIdentity([new Claim("actor", "replacement")], "test");

            Catch.Exception(() => evaluator.IsAuthorized(typeof(ProtectedCommand))).ShouldBeOfExactType<AsynchronousAuthorizationRequired>();
        }
    }

    [Fact]
    void should_reuse_the_marker_for_an_equivalent_standard_clone()
    {
        var (evaluator, accessor, declaration, principal) = Create();
        principal.Identities.Single().Actor = new ClaimsIdentity([new Claim("actor", "same")], "test");
        using (AuthorizationEvaluator.AlreadyEvaluated(typeof(ProtectedCommand), principal, declaration))
        {
            accessor.Current.Returns(new ClaimsPrincipal(principal.Identities.Select(identity => identity.Clone())));

            evaluator.IsAuthorized(typeof(ProtectedCommand)).ShouldBeTrue();
        }
    }

    static (AuthorizationEvaluator Evaluator, ICurrentPrincipalAccessor Accessor, AuthorizationDeclaration Declaration, ClaimsPrincipal Principal) Create()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        var declaration = new AuthorizationDeclarations(anonymous, attributes).For(typeof(ProtectedCommand));

        return (new AuthorizationEvaluator(accessor, anonymous, attributes), accessor, declaration, principal);
    }

    [Authorize(Policy = "Allowed", Roles = "Admin")]
    public record ProtectedCommand;
}
