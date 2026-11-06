// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Authorization.given;

internal static class verified_callers
{
    internal static VerifiedCaller Person(string subject, params string[] scopes) =>
        new("1", CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new("credential"), null, new(subject), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(scopes), new HashSet<string>(), null);

    internal static ClaimsPrincipal PrincipalFor(VerifiedCaller caller) => VerifiedCallerPrincipal.Create(caller);

    internal static AuthorizationDeclarations Declarations()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        return new AuthorizationDeclarations(anonymous, attributes);
    }
}
