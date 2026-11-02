// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders;

public class when_the_provider_returns_no_key : Specification
{
    Exception _exception;

    void Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
        using var frame = AuthorizationCommandIdentity.Enter(principal);
        _exception = Catch.Exception(() => ReadModelKeyProviders.Resolve(new LookUpByAttribute<CallerSubject>(), new(typeof(object), typeof(object), principal)));
    }

    [Fact] void should_deny_resolution() => ((ReadModelKeyResolutionDenied)_exception).Reason.ShouldEqual(ReadModelKeyRefusal.Missing);
}
