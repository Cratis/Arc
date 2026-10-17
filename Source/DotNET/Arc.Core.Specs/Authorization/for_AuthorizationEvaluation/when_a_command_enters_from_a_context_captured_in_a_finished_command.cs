// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_command_enters_from_a_context_captured_in_a_finished_command : Specification
{
    Exception? _error;
    bool _entered;

    void Because()
    {
        var host = new object();
        ExecutionContext captured;
        using (AuthorizationCommandIdentity.Enter(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "outer")], "Default")), host))
        {
            AuthorizationCommandIdentity.MarkTransactional();
            captured = ExecutionContext.Capture()!;
        }

        _error = Catch.Exception(() => ExecutionContext.Run(captured, EnterCommand, host));
    }

    [Fact] void should_not_reject_the_new_identity() => _error.ShouldBeNull();
    [Fact] void should_enter_the_new_command() => _entered.ShouldBeTrue();

    void EnterCommand(object? host)
    {
        using var frame = AuthorizationCommandIdentity.Enter(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "other")], "Default")), host);
        _entered = true;
    }
}
