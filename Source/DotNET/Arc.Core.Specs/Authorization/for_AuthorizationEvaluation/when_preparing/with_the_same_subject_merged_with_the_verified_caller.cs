// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation.when_preparing;

public class with_the_same_subject_merged_with_the_verified_caller : given.a_prepared_request
{
    ClaimsPrincipal _result;

    void Establish()
    {
        _selectedByScheme = new ClaimsPrincipal(_verified.Identities.Single());
        _selectedByScheme.AddIdentity(new ClaimsIdentity([new Claim("sub", "person-1"), new Claim(ClaimTypes.Role, "editor")], "other"));
        _selectedByScheme.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role, "reviewer")]));
    }

    async Task Because() => _result = (await PrepareSelectedPrincipal())!;

    [Fact] void should_keep_the_selection() => _result.ShouldEqual(_selectedByScheme);

    [Fact] void should_keep_the_role_of_the_same_subject() => _result.IsInRole("editor").ShouldBeTrue();

    [Fact] void should_keep_an_unauthenticated_transformation_identity() => _result.IsInRole("reviewer").ShouldBeTrue();
}
