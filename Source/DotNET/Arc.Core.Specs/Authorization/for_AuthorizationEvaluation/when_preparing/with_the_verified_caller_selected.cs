// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation.when_preparing;

public class with_the_verified_caller_selected : given.a_prepared_request
{
    ClaimsPrincipal? _result;

    void Establish() => _selectedByScheme = _verified;

    async Task Because() => _result = await PrepareSelectedPrincipal();

    [Fact] void should_keep_the_selected_principal() => _result.ShouldEqual(_verified);
}
