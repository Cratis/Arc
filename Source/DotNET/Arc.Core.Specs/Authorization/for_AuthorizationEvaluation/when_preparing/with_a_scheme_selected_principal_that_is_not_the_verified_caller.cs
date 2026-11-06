// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation.when_preparing;

public class with_a_scheme_selected_principal_that_is_not_the_verified_caller : given.a_prepared_request
{
    ClaimsPrincipal _result;

    void Establish() => _selectedByScheme = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "person-1")], "other"));

    async Task Because() => _result = (await PrepareSelectedPrincipal())!;

    [Fact] void should_replace_the_selection_with_an_anonymous_principal() => _result.Identity!.IsAuthenticated.ShouldBeFalse();

    [Fact] void should_not_merge_the_other_identity() => _result.Identities.Count().ShouldEqual(1);
}
