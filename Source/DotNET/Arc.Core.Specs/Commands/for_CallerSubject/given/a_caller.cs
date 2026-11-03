// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CallerSubject.given;

public class a_caller : Specification
{
    protected ClaimsPrincipal _principal;
    protected ReadModelKeyLookup _result;

    void Establish() => _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test"));

    protected ReadModelKeyLookup Resolve() => new CallerSubject().Resolve(new ReadModelKeyContext(CommandCaller.From(AuthorizationPrincipalIdentity.Capture(_principal)), typeof(object)));
}
