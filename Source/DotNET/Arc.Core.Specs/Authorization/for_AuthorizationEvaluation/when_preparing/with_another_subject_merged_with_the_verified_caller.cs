// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation.when_preparing;

public class with_another_subject_merged_with_the_verified_caller : given.a_prepared_request
{
    ClaimsPrincipal _result;

    void Establish()
    {
        _selectedByScheme = new ClaimsPrincipal(_verified.Identities.Single());
        _selectedByScheme.AddIdentity(new ClaimsIdentity([new Claim("sub", "someone-else"), new Claim(ClaimTypes.Role, "admin")], "other"));
    }

    async Task Because() => _result = (await PrepareSelectedPrincipal())!;

    [Fact] void should_refuse_by_selecting_an_anonymous_principal() => _result.Identity!.IsAuthenticated.ShouldBeFalse();

    [Fact] void should_not_carry_the_role_of_the_other_subject() => _result.IsInRole("admin").ShouldBeFalse();
}
