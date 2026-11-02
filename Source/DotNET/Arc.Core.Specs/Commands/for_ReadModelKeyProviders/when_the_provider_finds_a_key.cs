// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders;

public class when_the_provider_finds_a_key : Specification
{
    object _key;

    void Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test"));
        using var frame = AuthorizationCommandIdentity.Enter(principal);
        _key = ReadModelKeyProviders.Resolve(new LookUpByAttribute<CallerSubject>(), typeof(object), typeof(object), principal);
    }

    [Fact] void should_return_the_key() => _key.ShouldEqual("represented");
}
