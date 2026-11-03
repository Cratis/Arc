// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.given;

public class a_captured_caller : Specification
{
    protected ClaimsPrincipal _principal;
    protected IDisposable _frame;
    protected bool _success;
    protected CommandCaller _caller;
    protected ReadModelKeyRefusal _refusal;

    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test"));
        _frame = AuthorizationCommandIdentity.Enter(_principal);
    }

    void Destroy() => _frame.Dispose();
}
