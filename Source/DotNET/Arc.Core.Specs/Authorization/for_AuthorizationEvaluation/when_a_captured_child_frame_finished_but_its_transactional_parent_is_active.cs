// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_captured_child_frame_finished_but_its_transactional_parent_is_active : Specification
{
    Exception? _error;

    void Because()
    {
        var host = new object();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "outer")], "Default"));
        using var parent = AuthorizationCommandIdentity.Enter(principal, host);
        AuthorizationCommandIdentity.MarkTransactional();
        ExecutionContext captured;
        using (AuthorizationCommandIdentity.Enter(principal, host))
        {
            captured = ExecutionContext.Capture()!;
        }

        _error = Catch.Exception(() => ExecutionContext.Run(captured, EnterAsOther, host));
    }

    static void EnterAsOther(object? state)
    {
        using var frame = AuthorizationCommandIdentity.Enter(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "other")], "Default")), state);
    }

    [Fact] void should_reject_changing_the_active_parent_identity() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
}
